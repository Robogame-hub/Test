using System.Collections.Generic;
using UnityEngine;
using TankGame.Core;

namespace TankGame.Utils
{
    /// <summary>
    /// Универсальный пул объектов для оптимизации производительности
    /// </summary>
    /// <typeparam name="T">Тип компонента объекта в пуле</typeparam>
    public class ObjectPool<T> where T : Component
    {
        private readonly Queue<T> availableObjects = new Queue<T>();
        private readonly HashSet<T> activeObjects = new HashSet<T>();
        private readonly T prefab;
        private readonly Transform parent;
        private readonly int initialSize;
        private readonly bool expandable;

        public int ActiveCount => activeObjects.Count;
        public int AvailableCount => availableObjects.Count;
        public int TotalCount => ActiveCount + AvailableCount;

        public ObjectPool(T prefab, int initialSize = 10, Transform parent = null, bool expandable = true)
        {
            this.prefab = prefab;
            this.initialSize = initialSize;
            this.parent = parent;
            this.expandable = expandable;

            Initialize();
        }

        private void Initialize()
        {
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        private T CreateNewObject()
        {
            T obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            availableObjects.Enqueue(obj);
            return obj;
        }

        public T Get()
        {
            T obj;

            if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
            }
            else if (expandable)
            {
                obj = CreateNewObject();
                availableObjects.Dequeue(); // Удаляем только что добавленный
            }
            else
            {
                Debug.LogWarning($"ObjectPool<{typeof(T).Name}>: Pool is empty and not expandable!");
                return null;
            }

            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);

            // Вызываем OnSpawnFromPool если объект реализует IPoolable
            if (obj is IPoolable poolable)
            {
                poolable.OnSpawnFromPool();
            }

            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("Trying to return null object to pool");
                return;
            }

            if (!activeObjects.Contains(obj))
            {
                Debug.LogWarning($"ObjectPool<{typeof(T).Name}>: Object is not from this pool!");
                return;
            }

            // Вызываем OnReturnToPool если объект реализует IPoolable
            if (obj is IPoolable poolable)
            {
                poolable.OnReturnToPool();
            }

            obj.gameObject.SetActive(false);
            activeObjects.Remove(obj);
            availableObjects.Enqueue(obj);
        }

        public void ReturnAll()
        {
            var objectsToReturn = new List<T>(activeObjects);
            foreach (var obj in objectsToReturn)
            {
                Return(obj);
            }
        }

        public void Clear()
        {
            foreach (var obj in activeObjects)
            {
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }

            foreach (var obj in availableObjects)
            {
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }

            activeObjects.Clear();
            availableObjects.Clear();
        }
    }
}

