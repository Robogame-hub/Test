# Tank Game - Онлайн сражения на танках

## 🎮 Быстрый старт

### Настройка нового танка

1. Создайте GameObject для танка
2. Добавьте компоненты в следующем порядке:
   ```
   - TankMovement
   - TankTurret
   - TankWeapon
   - TankHealth
   - TankController_New
   ```

3. Настройте ссылки на дочерние объекты:
   ```
   Tank (GameObject)
   ├── Body (модель корпуса)
   ├── Turret (модель башни)
   │   └── Cannon (модель пушки)
   │       └── FirePoint (точка выстрела)
   └── Wheels (колеса)
   ```

4. Назначьте префаб пули в TankWeapon
5. Готово! Танк полностью функционален

## 🎯 Основной функционал

### Управление
- **W/S** - движение вперед/назад
- **A/D** - поворот танка
- **ПКМ (зажать)** - прицеливание
- **Движение мыши** - вращение башни и пушки
- **ЛКМ (при прицеливании)** - выстрел

### Механики

#### Движение
- Танк выравнивается по поверхности земли
- Плавное ускорение и торможение
- Инерция при повороте

#### Прицеливание
- Стабильность прицела увеличивается при неподвижной мыши
- Разброс уменьшается с увеличением стабильности
- Стабильность сбрасывается после выстрела

#### Стрельба
- Кулдаун между выстрелами
- Пули используют Object Pool (нет создания/уничтожения)
- Автоматическое уничтожение пуль по истечении времени
- VFX эффекты выстрела и попадания

## 📦 Компоненты

### TankMovement
Отвечает за движение танка.

**Параметры:**
- `moveSpeed` - скорость движения (м/с)
- `rotationSpeed` - скорость поворота (град/с)
- `groundCheckDistance` - дистанция проверки земли
- `groundAlignSpeed` - скорость выравнивания по земле
- `groundMask` - слой земли

### TankTurret
Отвечает за башню и прицеливание.

**Параметры:**
- `turret` - ссылка на Transform башни
- `turretRotationSpeed` - скорость вращения башни
- `cannon` - ссылка на Transform пушки
- `cannonRotationSpeed` - скорость наклона пушки
- `minCannonAngle` / `maxCannonAngle` - углы наклона пушки
- `crosshair` - прицел (опционально)
- `maxAimStability` - максимальная стабильность
- `stabilityIncreaseRate` - скорость увеличения стабильности
- `stabilityDecreaseRate` - скорость уменьшения стабильности

### TankWeapon
Отвечает за стрельбу.

**Параметры:**
- `firePoint` - точка выстрела
- `bulletPrefab` - префаб пули
- `bulletSpeed` - скорость пули
- `fireCooldown` - задержка между выстрелами
- `bulletLifetime` - время жизни пули
- `bulletPoolSize` - размер пула пуль
- `minSpreadAngle` / `maxSpreadAngle` - разброс
- `muzzleVFX` - эффект дульной вспышки
- `impactVFX` - эффект попадания

### TankHealth
Отвечает за здоровье.

**Параметры:**
- `maxHealth` - максимальное здоровье
- `canRegenerate` - включить регенерацию
- `regenerationRate` - скорость регенерации (HP/сек)
- `regenerationDelay` - задержка перед регенерацией

**События:**
- `OnHealthChanged(current, max)` - изменение здоровья
- `OnDamageTaken(hitPoint, hitNormal)` - получение урона
- `OnDeath()` - смерть

## 🌐 Сетевая игра

### Подключение Mirror

```csharp
using Mirror;
using TankGame.Tank;
using TankGame.Commands;

public class TankNetworkMirror : NetworkBehaviour
{
    private TankController_New tankController;
    
    void Start() {
        tankController = GetComponent<TankController_New>();
        tankController.IsLocalPlayer = isLocalPlayer;
    }
    
    void Update() {
        if (!isLocalPlayer) return;
        
        var input = GetComponent<TankInputHandler>().GetCurrentInput();
        CmdProcessInput(input);
    }
    
    [Command]
    void CmdProcessInput(TankInputCommand input) {
        tankController.ProcessCommand(input);
        RpcProcessInput(input);
    }
    
    [ClientRpc]
    void RpcProcessInput(TankInputCommand input) {
        if (isLocalPlayer) return;
        tankController.ProcessCommand(input);
    }
}
```

### Подключение Netcode for GameObjects

```csharp
using Unity.Netcode;
using TankGame.Tank;
using TankGame.Commands;

public class TankNetworkNetcode : NetworkBehaviour
{
    private TankController_New tankController;
    
    void Start() {
        tankController = GetComponent<TankController_New>();
        tankController.IsLocalPlayer = IsOwner;
    }
    
    void Update() {
        if (!IsOwner) return;
        
        var input = GetComponent<TankInputHandler>().GetCurrentInput();
        ProcessInputServerRpc(input);
    }
    
    [ServerRpc]
    void ProcessInputServerRpc(TankInputCommand input) {
        tankController.ProcessCommand(input);
        ProcessInputClientRpc(input);
    }
    
    [ClientRpc]
    void ProcessInputClientRpc(TankInputCommand input) {
        if (IsOwner) return;
        tankController.ProcessCommand(input);
    }
}
```

### Подключение Photon PUN

```csharp
using Photon.Pun;
using TankGame.Tank;
using TankGame.Commands;

public class TankNetworkPhoton : MonoBehaviourPun
{
    private TankController_New tankController;
    
    void Start() {
        tankController = GetComponent<TankController_New>();
        tankController.IsLocalPlayer = photonView.IsMine;
    }
    
    void Update() {
        if (!photonView.IsMine) return;
        
        var input = GetComponent<TankInputHandler>().GetCurrentInput();
        photonView.RPC("ProcessInput", RpcTarget.All, input);
    }
    
    [PunRPC]
    void ProcessInput(TankInputCommand input) {
        tankController.ProcessCommand(input);
    }
}
```

## 🔧 Кастомизация

### Добавление нового типа пули

```csharp
using TankGame.Weapons;

public class ExplosiveBullet : Bullet
{
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 50f;
    
    protected override void HandleImpact(Vector3 hitPoint, Vector3 hitNormal, GameObject hitObject)
    {
        // Взрыв
        Collider[] colliders = Physics.OverlapSphere(hitPoint, explosionRadius);
        foreach (var col in colliders)
        {
            var damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(explosionDamage, hitPoint, hitNormal);
            }
        }
        
        base.HandleImpact(hitPoint, hitNormal, hitObject);
    }
}
```

### Добавление бонусов

```csharp
public class HealthPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 25f;
    
    void OnTriggerEnter(Collider other)
    {
        var health = other.GetComponent<TankHealth>();
        if (health != null && health.IsAlive())
        {
            health.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
```

### Добавление нового состояния

```csharp
using TankGame.Tank.States;

public class TankBoostState : ITankState
{
    private TankController_New tank;
    private float boostMultiplier = 2f;
    private float originalSpeed;
    
    public TankBoostState(TankController_New tank) {
        this.tank = tank;
    }
    
    public void Enter() {
        originalSpeed = tank.Movement.MoveSpeed;
        // Увеличиваем скорость (через рефлексию или сделать сеттер)
    }
    
    public void Update() { }
    public void FixedUpdate() { }
    
    public void Exit() {
        // Возвращаем обратно
    }
}
```

## ⚡ Оптимизация

### Настройки для лучшей производительности

1. **Размер пула пуль:** увеличьте если видите создание новых пуль
   ```csharp
   bulletPoolSize = 50; // Больше для интенсивных перестрелок
   ```

2. **Частота сетевой синхронизации:**
   ```csharp
   networkSyncRate = 20f; // 20 Гц - баланс между точностью и трафиком
   ```

3. **Время жизни пуль:**
   ```csharp
   bulletLifetime = 3f; // Меньше = меньше активных объектов
   ```

4. **Интерполяция:**
   ```csharp
   interpolationBackTime = 0.1f; // 100ms - стандарт
   ```

### WebGL оптимизации

- Object Pooling используется автоматически
- Избегайте создания объектов в runtime
- Используйте меньше ParticleSystem'ов
- Ограничьте количество одновременных пуль

## 📊 Отладка

### Включение визуализации

В `TankController_New` метод `OnDrawGizmos` показывает:
- Зеленая линия = направление танка
- Красная линия = направление башни

### Логирование

Добавьте в свой код:
```csharp
void OnEnable() {
    tankHealth.OnDamageTaken.AddListener((pos, normal) => {
        Debug.Log($"Получен урон в позиции {pos}");
    });
}
```

## 📋 Чек-лист перед билдом

- [ ] Все компоненты назначены на танке
- [ ] Prefab пули создан и назначен
- [ ] FirePoint находится на правильной позиции
- [ ] Ground Layer установлен правильно
- [ ] VFX эффекты назначены (опционально)
- [ ] Размер пула достаточный для игры
- [ ] Сетевые компоненты настроены (для онлайн)

## 🐛 Частые проблемы

**Танк проваливается сквозь землю**
- Проверьте Ground Layer Mask
- Увеличьте groundCheckDistance

**Пули не появляются**
- Проверьте назначение bulletPrefab
- Проверьте позицию FirePoint

**Лаги в сетевой игре**
- Уменьшите networkSyncRate
- Используйте ClientPrediction и NetworkInterpolation
- Проверьте размер пакетов данных

**Низкий FPS на WebGL**
- Уменьшите bulletPoolSize
- Отключите сложные VFX
- Уменьшите количество игроков

## 📚 Дополнительная информация

См. [ARCHITECTURE.md](ARCHITECTURE.md) для детального описания архитектуры.

## 📄 Лицензия

Код свободен для использования в ваших проектах.

