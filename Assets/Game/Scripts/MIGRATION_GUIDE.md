# Руководство по миграции

## Переход со старой архитектуры на новую

### Зачем мигрировать?

**Старая архитектура (TankController.cs):**
- ❌ Монолитный класс (300+ строк)
- ❌ Сложно тестировать
- ❌ FindObjectOfType каждый кадр (медленно)
- ❌ Не готово к сетевой игре
- ❌ Сложно расширять

**Новая архитектура (TankController_New.cs):**
- ✅ Модульные компоненты
- ✅ Легко тестировать
- ✅ Оптимизировано (кэшированные ссылки)
- ✅ Готово к сетевой игре
- ✅ Легко расширять

### Сравнение кода

#### Старый код
```csharp
public class TankController : MonoBehaviour
{
    // 50+ полей в одном классе
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private Transform turret;
    [SerializeField] private Transform cannon;
    [SerializeField] private GameObject bulletPrefab;
    // ... еще 45 полей
    
    void Update()
    {
        HandleMovement();      // 20 строк
        HandleTurretRotation(); // 40 строк
        HandleShooting();       // 30 строк
        UpdateBulletLifetime(); // 10 строк
        AlignToGround();        // 25 строк
    }
}
```

#### Новый код
```csharp
public class TankController_New : MonoBehaviour
{
    // Только ссылки на компоненты
    [SerializeField] private TankMovement movement;
    [SerializeField] private TankTurret turret;
    [SerializeField] private TankWeapon weapon;
    [SerializeField] private TankHealth health;
    
    void Update()
    {
        // Каждый компонент управляет собой
        ProcessLocalInput();
        movement.AlignToGround();
        
        if (ShouldSyncNetwork())
            SyncToNetwork();
    }
}
```

### Пошаговая миграция

#### Шаг 1: Создайте резервную копию

1. Сохраните сцену
2. Сделайте коммит в Git (если используете)
3. Или скопируйте папку проекта

#### Шаг 2: Подготовка префаба танка

**Было:**
```
Tank (TankController)
├── Turret
│   └── Cannon
│       └── FirePoint
└── ...
```

**Стало:**
```
Tank (TankController_New + компоненты)
├── Turret
│   └── Cannon
│       └── FirePoint
└── ...
```

#### Шаг 3: Замена компонентов

1. Откройте префаб/объект танка в сцене

2. **НЕ УДАЛЯЙТЕ** старый TankController (пока)

3. Добавьте новые компоненты:
   ```
   Add Component -> TankMovement
   Add Component -> TankTurret  
   Add Component -> TankWeapon
   Add Component -> TankHealth
   Add Component -> TankInputHandler
   Add Component -> TankController_New
   ```

4. Скопируйте значения полей:

   **Из TankController в TankMovement:**
   - Move Speed → Move Speed
   - Rotation Speed → Rotation Speed
   - Ground Check Distance → Ground Check Distance
   - Ground Align Speed → Ground Align Speed
   - Ground Mask → Ground Mask

   **Из TankController в TankTurret:**
   - Turret → Turret
   - Turret Rotation Speed → Turret Rotation Speed
   - Cannon → Cannon
   - Cannon Rotation Speed → Cannon Rotation Speed
   - Min Cannon Angle → Min Cannon Angle
   - Max Cannon Angle → Max Cannon Angle
   - Crosshair → Crosshair
   - Max Aim Stability → Max Aim Stability
   - Stability Increase Rate → Stability Increase Rate
   - Stability Decrease Rate → Stability Decrease Rate

   **Из TankController в TankWeapon:**
   - Fire Point → Fire Point
   - Bullet Prefab → Bullet Prefab (используйте новый Bullet!)
   - Bullet Speed → Bullet Speed
   - Fire Cooldown → Fire Cooldown
   - Bullet Lifetime → Bullet Lifetime
   - Bullet Pool Size → Bullet Pool Size
   - Min Spread Angle → Min Spread Angle
   - Max Spread Angle → Max Spread Angle
   - Muzzle VFX → Muzzle VFX
   - Impact VFX → Impact VFX

5. В TankController_New назначьте ссылки на компоненты:
   - Movement → TankMovement (автоматически)
   - Turret → TankTurret (автоматически)
   - Weapon → TankWeapon (автоматически)
   - Health → TankHealth (автоматически)
   - Input Handler → TankInputHandler (автоматически)

6. Уберите галочку с TankController (отключите)

7. Протестируйте танк

8. Если все работает - удалите TankController

#### Шаг 4: Миграция пули

**Старая пуля:**
```csharp
// Assets/Game/Scripts/Bullet.cs
public class Bullet : MonoBehaviour
{
    // FindObjectOfType каждый раз!
    if (tankController == null)
        tankController = FindFirstObjectByType<TankController>();
}
```

**Новая пуля:**
```csharp
// Assets/Game/Scripts/Weapons/Bullet_New.cs
namespace TankGame.Weapons
{
    public class Bullet : MonoBehaviour, IPoolable
    {
        // Передается при создании
        public void Initialize(TankWeapon weapon, ...)
    }
}
```

**Действия:**

1. Создайте новый префаб пули на основе старого
2. Замените скрипт Bullet на Bullet (новый из namespace TankGame.Weapons)
3. Назначьте новый префаб в TankWeapon

#### Шаг 5: Применение к префабу

1. Если танк - это префаб, примените изменения:
   - Правый клик на объект в иерархии
   - Overrides → Apply All

2. Сохраните сцену

#### Шаг 6: Обновление существующих сцен

Если танк используется в нескольких сценах:

1. Откройте каждую сцену
2. Удалите старый танк
3. Добавьте обновленный префаб танка

### Автоматический скрипт миграции

```csharp
using UnityEngine;
using UnityEditor;

public class TankMigrationTool : EditorWindow
{
    [MenuItem("Tools/Migrate Tank to New Architecture")]
    static void MigrateTank()
    {
        var oldTank = FindObjectOfType<TankController>();
        if (oldTank == null)
        {
            Debug.LogError("Старый TankController не найден!");
            return;
        }

        GameObject tankObj = oldTank.gameObject;

        // Добавляем новые компоненты
        var movement = tankObj.AddComponent<TankMovement>();
        var turret = tankObj.AddComponent<TankTurret>();
        var weapon = tankObj.AddComponent<TankWeapon>();
        var health = tankObj.AddComponent<TankHealth>();
        var newController = tankObj.AddComponent<TankController_New>();

        // Копируем значения через SerializedObject
        CopySerializedFields(oldTank, movement);
        CopySerializedFields(oldTank, turret);
        CopySerializedFields(oldTank, weapon);

        // Отключаем старый компонент
        oldTank.enabled = false;

        Debug.Log("Миграция завершена! Проверьте танк и удалите старый TankController.");
    }

    static void CopySerializedFields(Object source, Object target)
    {
        SerializedObject soSource = new SerializedObject(source);
        SerializedObject soTarget = new SerializedObject(target);

        SerializedProperty prop = soSource.GetIterator();
        while (prop.NextVisible(true))
        {
            SerializedProperty targetProp = soTarget.FindProperty(prop.name);
            if (targetProp != null && targetProp.propertyType == prop.propertyType)
            {
                soTarget.CopyFromSerializedProperty(prop);
            }
        }

        soTarget.ApplyModifiedProperties();
    }
}
```

**Как использовать:**
1. Создайте файл `Assets/Editor/TankMigrationTool.cs`
2. Вставьте код выше
3. Выберите танк в сцене
4. Menu → Tools → Migrate Tank to New Architecture

### Проверка после миграции

✅ **Проверьте:**
- [ ] Танк двигается как раньше
- [ ] Башня вращается
- [ ] Стрельба работает
- [ ] Пули исчезают правильно
- [ ] VFX эффекты проигрываются
- [ ] Нет ошибок в консоли
- [ ] Производительность не ухудшилась (должна улучшиться!)

### Откат назад

Если что-то пошло не так:

1. Включите обратно старый TankController
2. Удалите новые компоненты
3. Восстановите из Git / резервной копии

### Постмиграционные улучшения

После миграции вы можете:

1. **Добавить сетевую игру:**
   ```csharp
   // Добавьте NetworkManager
   // Добавьте ClientPrediction
   // Добавьте NetworkInterpolation
   ```

2. **Добавить систему состояний:**
   ```csharp
   var stateMachine = tankObj.AddComponent<TankStateMachine>();
   stateMachine.ChangeState(new TankAliveState(newController));
   ```

3. **Оптимизировать дальше:**
   - Увеличьте размер пула пуль
   - Настройте сетевую синхронизацию
   - Добавьте LOD для моделей

## Частые вопросы

**Q: Можно ли использовать оба варианта одновременно?**
A: Да, старый код помечен `[Obsolete]` но работает. Но лучше мигрировать полностью.

**Q: Нужно ли переделывать все танки сразу?**
A: Нет, можете мигрировать постепенно. Сначала один танк, протестировать, потом остальные.

**Q: Что делать с кастомными модификациями старого кода?**
A: Перенесите логику в соответствующий компонент или создайте новый компонент.

**Q: Производительность действительно улучшится?**
A: Да! Особенно на WebGL. Убрали FindObjectOfType, улучшили Object Pool, оптимизировали обновления.

**Q: Сломаются ли сохранения/replays?**
A: Если у вас есть система сохранений - да, нужно обновить. Но новая архитектура упрощает это (см. Command Pattern).

## Поддержка

Если возникли проблемы:
1. Проверьте консоль Unity на ошибки
2. Убедитесь что все ссылки назначены
3. Сверьтесь с примером в README.md
4. Проверьте ARCHITECTURE.md для понимания как работает новая система

