using UnityEngine;
using System.Collections.Generic;

public class TankController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Turret Settings")]
    [SerializeField] private Transform turret;
    [SerializeField] private float turretRotationSpeed = 60f;

    [Header("Cannon Settings")]
    [SerializeField] private Transform cannon;
    [SerializeField] private float cannonRotationSpeed = 60f;
    [SerializeField] private float minCannonAngle = -30f;
    [SerializeField] private float maxCannonAngle = 10f;

    [Header("Aiming Settings")]
    [SerializeField] private GameObject crosshair;
    [SerializeField] private float maxAimStability = 1f;
    [SerializeField] private float stabilityIncreaseRate = 0.5f;
    [SerializeField] private float stabilityDecreaseRate = 2f;
    [SerializeField] private float minSpreadAngle = 0.5f;
    [SerializeField] private float maxSpreadAngle = 5f;

    [Header("Shooting Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float fireCooldown = 0.5f;
    [SerializeField] private float bulletLifetime = 5f;
    [SerializeField] private int bulletPoolSize = 20;
    [SerializeField] private Transform bulletPoolParent;
    [SerializeField] private GameObject impactVFX;

    [Header("VFX")]
    [SerializeField] private GameObject muzzleVFX;

    [Header("Ground Alignment")]
    [SerializeField] private float groundCheckDistance = 3f;
    [SerializeField] private float groundAlignSpeed = 8f;
    [SerializeField] private LayerMask groundMask;

    private Rigidbody rb;

    private Queue<GameObject> bulletPool = new();
    private List<GameObject> activeBullets = new();
    private Dictionary<GameObject, float> bulletSpawnTimes = new();

    private float currentStability;
    private float lastFireTime;
    private bool isRightMouseDown;
    private Vector2 lastMousePosition;
    private float turretRotationVelocity;

    // 🔑 КЛЮЧЕВОЕ
    private float currentYaw;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.freezeRotation = true;
        rb.useGravity = true;

        currentYaw = transform.eulerAngles.y;

        if (crosshair)
            crosshair.SetActive(false);

        InitializeTransforms();
        InitializeBulletPool();
    }

    private void Update()
    {
        HandleMovement();
        HandleTurretRotation();
        HandleShooting();
        UpdateBulletLifetime();
        AlignToGround();
    }

    #region Movement & Ground

    private void HandleMovement()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        Vector3 moveDirection = transform.forward * -vertical;
        rb.linearVelocity = moveDirection * moveSpeed;

        // ❗ ТОЛЬКО накопление yaw
        currentYaw += horizontal * rotationSpeed * Time.deltaTime;
    }

    private void AlignToGround()
    {
        Ray ray = new Ray(transform.position + Vector3.up, Vector3.down);

        if (!Physics.Raycast(ray, out RaycastHit hit, groundCheckDistance, groundMask))
            return;

        Vector3 groundNormal = hit.normal;

        Quaternion yawRotation = Quaternion.Euler(0f, currentYaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;

        Vector3 alignedForward = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
        if (alignedForward.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(alignedForward, groundNormal);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            groundAlignSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Turret & Shooting

    private void HandleTurretRotation()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRightMouseDown = true;
            lastMousePosition = Input.mousePosition;
            if (crosshair) crosshair.SetActive(true);
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRightMouseDown = false;
            if (crosshair) crosshair.SetActive(false);
            currentStability = 0f;
        }

        if (!isRightMouseDown)
            return;

        Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;
        float totalMovement = 0f;

        if (turret)
        {
            float rot = mouseDelta.x * turretRotationSpeed * Time.deltaTime;
            turret.Rotate(0f, 0f, rot);
            totalMovement += Mathf.Abs(rot);
        }

        if (cannon)
        {
            float rot = mouseDelta.y * cannonRotationSpeed * Time.deltaTime;
            float angle = cannon.localEulerAngles.x;
            if (angle > 180f) angle -= 360f;

            float newAngle = Mathf.Clamp(angle + rot, minCannonAngle, maxCannonAngle);
            cannon.localRotation = Quaternion.Euler(newAngle, 0f, 0f);
            totalMovement += Mathf.Abs(rot);
        }

        if (totalMovement > 0.01f)
        {
            turretRotationVelocity = totalMovement;
            currentStability = Mathf.Max(0f, currentStability - stabilityDecreaseRate * Time.deltaTime);
        }
        else
        {
            turretRotationVelocity = Mathf.Lerp(turretRotationVelocity, 0f, Time.deltaTime * 5f);
            if (turretRotationVelocity < 0.1f)
                currentStability = Mathf.Min(maxAimStability, currentStability + stabilityIncreaseRate * Time.deltaTime);
        }

        lastMousePosition = Input.mousePosition;
    }

    private void HandleShooting()
    {
        if (isRightMouseDown && Input.GetMouseButtonDown(0))
        {
            if (Time.time - lastFireTime >= fireCooldown)
            {
                Fire();
                lastFireTime = Time.time;
            }
        }
    }

    private void Fire()
    {
        float spread = Mathf.Lerp(maxSpreadAngle, minSpreadAngle, currentStability);
        float angle = Random.Range(-spread, spread);

        Vector3 direction = firePoint.up;
        direction = Quaternion.AngleAxis(angle, firePoint.forward) * direction;

        PlayMuzzleVFX();

        GameObject bullet = GetBulletFromPool();
        bullet.transform.SetPositionAndRotation(
            firePoint.position,
            Quaternion.LookRotation(direction, firePoint.forward)
        );

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript && impactVFX)
            bulletScript.SetImpactEffect(impactVFX);

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.linearVelocity = direction * bulletSpeed;

        currentStability = 0f;
    }

    private void PlayMuzzleVFX()
    {
        if (!muzzleVFX || !firePoint) return;

        GameObject vfx = Instantiate(muzzleVFX, firePoint.position, firePoint.rotation, firePoint);
        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();

        if (ps && !ps.main.loop)
            Destroy(vfx, ps.main.duration + ps.main.startLifetime.constantMax);
    }

    #endregion

    #region Bullet Pool

    private void InitializeBulletPool()
    {
        if (!bulletPrefab) return;

        if (!bulletPoolParent)
            bulletPoolParent = new GameObject("BulletPool").transform;

        for (int i = 0; i < bulletPoolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, bulletPoolParent);
            bullet.SetActive(false);
            bulletPool.Enqueue(bullet);
        }
    }

    private GameObject GetBulletFromPool()
    {
        GameObject bullet = bulletPool.Count > 0
            ? bulletPool.Dequeue()
            : Instantiate(bulletPrefab, bulletPoolParent);

        bullet.SetActive(true);
        activeBullets.Add(bullet);
        bulletSpawnTimes[bullet] = Time.time;
        return bullet;
    }

    public void ReturnBulletToPool(GameObject bullet)
    {
        if (!bullet) return;

        bullet.SetActive(false);
        activeBullets.Remove(bullet);
        bulletSpawnTimes.Remove(bullet);
        bulletPool.Enqueue(bullet);
    }

    private void UpdateBulletLifetime()
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            GameObject bullet = activeBullets[i];
            if (!bullet) continue;

            if (Time.time - bulletSpawnTimes[bullet] >= bulletLifetime)
                ReturnBulletToPool(bullet);
        }
    }

    #endregion

    private void InitializeTransforms()
    {
        if (!turret)
            turret = transform.Find("Turret");

        if (!cannon && turret)
            cannon = turret.Find("Cannon") ?? turret;

        if (!firePoint && cannon)
            firePoint = cannon.Find("FirePoint") ?? cannon;
    }
}
