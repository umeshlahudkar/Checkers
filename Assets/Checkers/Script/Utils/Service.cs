using UnityEngine;

public abstract class Service<T> : MonoBehaviour where T : Component
{
    protected virtual void Awake()
    {
        if (ServiceLocator.TryGet<T>(out T existing) && existing != this)
        {
            Destroy(gameObject);
            return;
        }

        ServiceLocator.Register(this as T);
    }

    protected virtual void OnDestroy()
    {
        if (ServiceLocator.TryGet<T>(out T current) && current == this)
        {
            ServiceLocator.Unregister<T>();
        }
    }
}
