using UnityEngine;
using TankGame.Tank;
using TankGame.Tank.Components;

namespace TankGame.Game
{
    /// <summary>
    /// Точка спавна для танков
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Номер спавн-поинта (для идентификации)")]
        [SerializeField] private int spawnPointIndex = 0;
        
        [Tooltip("Визуализация точки спавна (опционально)")]
        [SerializeField] private GameObject visualMarker;
        
        private bool isOccupied = false;
        private TankController occupiedByTank = null;
        
        public int SpawnPointIndex => spawnPointIndex;
        public bool IsOccupied => isOccupied;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;
        
        /// <summary>
        /// Устанавливает занятость точки спавна
        /// </summary>
        public void SetOccupied(bool occupied, TankController tank = null)
        {
            isOccupied = occupied;
            occupiedByTank = tank;
            
            // Визуализация (скрыть/показать маркер)
            if (visualMarker != null)
            {
                visualMarker.SetActive(!occupied);
            }
        }
        
        /// <summary>
        /// Спавнит танк в этой точке
        /// </summary>
        public void SpawnTank(TankController tank)
        {
            if (tank == null)
                return;
            
            // Включаем танк (если был выключен)
            tank.gameObject.SetActive(true);
            
            // Устанавливаем позицию и поворот
            tank.transform.SetPositionAndRotation(Position, Rotation);
            
            // Активируем танк (включаем все компоненты)
            ActivateTank(tank);
            
            SetOccupied(true, tank);
            
            Debug.Log($"[SpawnPoint] Tank {tank.name} spawned at point {spawnPointIndex}");
        }
        
        /// <summary>
        /// Активирует танк (включает все компоненты для работы)
        /// </summary>
        private void ActivateTank(TankController tank)
        {
            // Включаем все компоненты танка
            if (tank != null)
            {
                tank.enabled = true;
            }
            
            // Включаем коллайдеры
            Collider[] colliders = tank.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = true;
            }
            
            // Включаем физику (сбрасываем velocity)
            Rigidbody rb = tank.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Включаем видимость танка
            Renderer[] renderers = tank.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = true;
            }
        }
        
        private void OnDrawGizmos()
        {
            // Визуализация в редакторе
            Gizmos.color = isOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Стрелка направления
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}

