# 🔍 Диагностика: Photon не подключается

## ❌ Проблема:
```
[PhotonDiagnostics] Connected: ✗ NO
[PhotonDiagnostics] In Room: ✗ NO
[PhotonNetworkManager] Start() - НЕТ ЛОГОВ!
```

Это означает, что `PhotonNetworkManager.Start()` НЕ вызывается или `Connect()` не вызывается.

---

## ✅ Решение:

### Шаг 1: Проверьте, есть ли PhotonNetworkManager в сцене

1. Откройте сцену `Assets/Scenes/Core.unity`
2. В **Hierarchy** найдите GameObject с именем `PhotonNetworkManager`
3. Если НЕТ:
   - Создайте пустой GameObject: **GameObject → Create Empty**
   - Назовите его `PhotonNetworkManager`
   - **Add Component → PhotonNetworkManager (TankGame.Network)**

### Шаг 2: Проверьте настройки PhotonNetworkManager

Выберите `PhotonNetworkManager` в Hierarchy и проверьте в Inspector:

- [ ] **Auto Connect On Start**: `☑` (должно быть включено!)
- [ ] **Room Name**: `MainRoom` (или другое имя)
- [ ] **Tank Prefab**: назначен префаб из `Resources/TANK`

### Шаг 3: Проверьте логи при запуске

В консоли Unity ДОЛЖНЫ быть логи:
```
[PhotonNetworkManager] Start(): Not connected, calling Connect()...
[PhotonNetworkManager] Calling PhotonNetwork.ConnectUsingSettings()...
```

Если этих логов НЕТ → `PhotonNetworkManager.Start()` не вызывается или `autoConnectOnStart = false`

### Шаг 4: Проверьте ошибки подключения

В консоли могут быть ошибки:
- `[PhotonNetworkManager] Photon App ID not configured!` → Нужно настроить App ID
- `[PhotonNetworkManager] Photon PUN 2 not installed!` → Photon не установлен
- Другие ошибки Photon → смотрите детали ниже

---

## 🔧 Частые причины:

### Причина 1: PhotonNetworkManager НЕТ в сцене

**Симптомы:** Нет логов `[PhotonNetworkManager]` вообще

**Решение:** Создайте GameObject с компонентом `PhotonNetworkManager` в сцене

### Причина 2: Auto Connect On Start выключен

**Симптомы:** Нет логов `Connect()` или `Start()`

**Решение:** В Inspector `PhotonNetworkManager` включите `Auto Connect On Start`

### Причина 3: Photon App ID не настроен

**Симптомы:** В консоли: `Photon App ID not configured!`

**Решение:**
1. **Photon → Pun → Wizard**
2. Заполните **App ID Realtime**
3. Нажмите **Setup Project**

### Причина 4: Photon PUN 2 не установлен

**Симптомы:** В консоли: `Photon PUN 2 not installed!`

**Решение:**
1. Установите Photon PUN 2 из Asset Store
2. Или проверьте Scripting Define Symbols в Player Settings

### Причина 5: Start In Offline Mode включен

**Симптомы:** Photon подключается, но не к реальному серверу

**Решение:**
1. **Photon → Pun → Wizard** → **Show Settings**
2. Установите **Start In Offline Mode**: `☐` (выключено)

---

## 📋 Чеклист для проверки:

- [ ] PhotonNetworkManager есть в сцене (GameObject в Hierarchy)
- [ ] PhotonNetworkManager имеет компонент `PhotonNetworkManager`
- [ ] **Auto Connect On Start**: `☑` (включено)
- [ ] **Room Name**: заполнен (например, `MainRoom`)
- [ ] **Tank Prefab**: назначен
- [ ] Photon App ID настроен в Photon Wizard
- [ ] Start In Offline Mode выключен

---

## 🧪 Быстрый тест:

1. Выберите `PhotonNetworkManager` в Hierarchy
2. В Inspector проверьте **Auto Connect On Start** = `☑`
3. Нажмите **Play**
4. В консоли ДОЛЖНЫ появиться логи:
   ```
   [PhotonNetworkManager] Start(): Not connected, calling Connect()...
   [PhotonNetworkManager] Calling PhotonNetwork.ConnectUsingSettings()...
   ```
5. Если этих логов НЕТ → проблема в настройках PhotonNetworkManager

---

## 💡 Дополнительная диагностика:

### Проверьте, вызывается ли Start():

Добавьте в начало `PhotonNetworkManager.Start()`:
```csharp
Debug.Log("[PhotonNetworkManager] Start() called! autoConnectOnStart=" + autoConnectOnStart);
```

Если этот лог не появляется → GameObject не активен или компонент не работает.

### Проверьте Photon Settings вручную:

1. Откройте **Photon → Pun → Wizard**
2. Проверьте **App ID Realtime** - должен быть заполнен
3. Нажмите **"Show Settings"**
4. Проверьте **Start In Offline Mode** - должен быть `☐` (выключен)

---

## ✅ Итог:

Если Photon не подключается, проверьте:
1. **PhotonNetworkManager в сцене** - самый частый случай!
2. **Auto Connect On Start включен**
3. **App ID настроен**
4. **Start In Offline Mode выключен**

После исправления перезапустите игру и проверьте логи!

