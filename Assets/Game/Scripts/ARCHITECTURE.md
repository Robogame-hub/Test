# Архитектура онлайн игры про танчики

## Обзор

Проект использует **модульную компонентную архитектуру** с применением следующих паттернов проектирования:

### Основные паттерны

1. **Component Pattern** - Разделение функционала танка на независимые компоненты
2. **Command Pattern** - Обработка пользовательского ввода и сетевая синхронизация
3. **State Pattern** - Управление состояниями танка
4. **Object Pool Pattern** - Оптимизация создания/уничтожения объектов
5. **Client-Server Architecture** - Авторитетный сервер с клиентским предсказанием

## Структура проекта

```
Assets/Game/Scripts/
├── Core/                       # Базовые интерфейсы
│   ├── IDamageable.cs         # Интерфейс для объектов с HP
│   ├── IPoolable.cs           # Интерфейс для объектов в пуле
│   └── INetworkSyncable.cs    # Интерфейс для сетевой синхронизации
│
├── Commands/                   # Command Pattern
│   ├── ICommand.cs            # Базовый интерфейс команды
│   └── TankInputCommand.cs    # Команда ввода танка
│
├── Tank/                       # Компоненты танка
│   ├── TankController_New.cs  # Главный контроллер (координатор)
│   ├── TankInputHandler.cs    # Обработка ввода
│   │
│   ├── Components/             # Модульные компоненты
│   │   ├── TankMovement.cs    # Движение
│   │   ├── TankTurret.cs      # Башня и прицеливание
│   │   ├── TankWeapon.cs      # Оружие и стрельба
│   │   └── TankHealth.cs      # Здоровье и урон
│   │
│   └── States/                 # State Pattern
│       ├── ITankState.cs      # Интерфейс состояния
│       ├── TankStateMachine.cs
│       ├── TankAliveState.cs
│       ├── TankDeadState.cs
│       └── TankStunnedState.cs
│
├── Weapons/                    # Оружие
│   └── Bullet_New.cs          # Оптимизированная пуля
│
├── Network/                    # Сетевая синхронизация
│   ├── NetworkManager.cs      # Сетевой менеджер
│   ├── ClientPrediction.cs    # Клиентское предсказание
│   └── NetworkInterpolation.cs # Интерполяция для удаленных игроков
│
└── Utils/                      # Утилиты
    └── ObjectPool.cs          # Универсальный пул объектов
```

## Принципы работы

### 1. Модульность (Component Pattern)

Танк состоит из независимых компонентов:
- **TankMovement** - только движение
- **TankTurret** - только башня
- **TankWeapon** - только стрельба
- **TankHealth** - только здоровье

**Преимущества:**
- Легко тестировать каждый компонент отдельно
- Можно переиспользовать компоненты
- Легко расширять функционал
- Соблюдается принцип единственной ответственности (SRP)

### 2. Command Pattern для ввода

```csharp
// Ввод преобразуется в команду
TankInputCommand input = inputHandler.GetCurrentInput();

// Команда может быть:
// - Выполнена локально
// - Отправлена по сети
// - Сохранена в истории
// - Переиграна для коррекции предсказания
tankController.ProcessCommand(input);
```

**Преимущества:**
- Легко записывать и воспроизводить игру (replays)
- Простая отправка по сети
- Поддержка отмены действий (undo)
- Клиентское предсказание

### 3. State Pattern для состояний

```csharp
// Танк может быть в разных состояниях
TankAliveState    // Живой - может двигаться
TankDeadState     // Мертвый - ждет респавна
TankStunnedState  // Оглушен - не может двигаться
```

**Преимущества:**
- Четкое разделение логики для каждого состояния
- Легко добавлять новые состояния
- Предотвращает баги (мертвый танк не может стрелять)

### 4. Object Pool для производительности

```csharp
// Вместо Instantiate/Destroy
ObjectPool<Bullet> bulletPool;
Bullet bullet = bulletPool.Get();  // Получить из пула
bulletPool.Return(bullet);         // Вернуть в пул
```

**Преимущества:**
- Нет GC (Garbage Collection) во время игры
- Критично для WebGL (медленный GC)
- Стабильный FPS

### 5. Client-Server архитектура

#### Клиентское предсказание (Client Prediction)
```
1. Игрок нажимает кнопку
2. Команда выполняется СРАЗУ локально (предсказание)
3. Команда отправляется серверу
4. Сервер обрабатывает и отправляет результат
5. Клиент сверяет с сервером и корректирует если нужно
```

#### Интерполяция (для других игроков)
```
1. Получаем позицию игрока от сервера
2. Сохраняем в буфер
3. Плавно интерполируем между позициями
4. Результат: плавное движение без рывков
```

## Оптимизация для WebGL

### 1. Избегаем FindObjectOfType каждый кадр
```csharp
// ❌ Плохо
void Update() {
    TankController tank = FindObjectOfType<TankController>();
}

// ✅ Хорошо
void Awake() {
    tankController = GetComponent<TankController>(); // 1 раз
}
```

### 2. Object Pooling для всех частых объектов
- Пули
- Эффекты (VFX)
- Декали от попаданий

### 3. Ограничение частоты отправки по сети
```csharp
[SerializeField] private float networkSyncRate = 20f; // 20 Гц вместо 60
```

### 4. Интерполяция вместо частых обновлений
- Получаем обновления реже
- Локально плавно интерполируем

## Интеграция с сетевыми фреймворками

Архитектура готова к интеграции с:

### Mirror
```csharp
public class TankNetworkMirror : NetworkBehaviour {
    [Command]
    void CmdProcessInput(TankInputCommand input) {
        tankController.ProcessCommand(input);
    }
}
```

### Netcode for GameObjects
```csharp
public class TankNetworkNetcode : NetworkBehaviour {
    void Update() {
        if (IsOwner) {
            var input = inputHandler.GetCurrentInput();
            ProcessInputServerRpc(input);
        }
    }
}
```

### Photon PUN
```csharp
public class TankNetworkPhoton : MonoBehaviourPun {
    void Update() {
        if (photonView.IsMine) {
            var input = inputHandler.GetCurrentInput();
            photonView.RPC("ProcessInput", RpcTarget.All, input);
        }
    }
}
```

## Расширяемость

### Добавление нового оружия
1. Создайте класс наследник от базового оружия
2. Переопределите метод Fire()
3. Готово!

### Добавление нового состояния
1. Создайте класс реализующий ITankState
2. Добавьте логику в Enter/Update/Exit
3. Используйте через TankStateMachine

### Добавление новой механики
1. Создайте новый компонент
2. Добавьте на танк
3. Обращайтесь через TankController

## Рекомендации

### Для локальной игры
Используйте только:
- TankController_New
- Компоненты (Movement, Turret, Weapon, Health)
- Object Pool

### Для онлайн игры
Дополнительно используйте:
- NetworkManager
- ClientPrediction
- NetworkInterpolation
- Интеграцию с выбранным фреймворком (Mirror/Netcode/Photon)

## Производительность

### Целевые показатели для WebGL
- **60 FPS** на средних ПК
- **30-60 FPS** на слабых ПК
- **Без фризов** при стрельбе (благодаря Object Pool)
- **Стабильный FPS** без скачков (минимум GC)

### Метрики сети
- **Клиентский ввод:** 30 Гц
- **Серверный тик:** 60 Гц  
- **Синхронизация состояния:** 20 Гц
- **Задержка интерполяции:** 100ms

## Заключение

Данная архитектура обеспечивает:
- ✅ Модульность и расширяемость
- ✅ Высокую производительность
- ✅ Готовность к сетевой игре
- ✅ Оптимизацию под WebGL
- ✅ Легкость в тестировании и отладке

