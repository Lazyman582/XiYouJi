using System;
using System.Collections.Generic;

namespace XiYouJi.Events
{
    public static class EventBus<TEvent>
    {
        private static readonly List<Listener> listeners = new List<Listener>();
        private static int nextOrder;

        private sealed class Listener
        {
            public Action<TEvent> Action;
            public int Priority;
            public int Order;
        }

        public static void Subscribe(Action<TEvent> listener, int priority = 0)
        {
            if (listener == null)
            {
                throw new ArgumentNullException("listener");
            }

            listeners.Add(new Listener
            {
                Action = listener,
                Priority = priority,
                Order = nextOrder++
            });

            listeners.Sort(CompareListeners);
        }

        public static void Unsubscribe(Action<TEvent> listener)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (listeners[i].Action == listener)
                {
                    listeners.RemoveAt(i);
                    return;
                }
            }
        }

        public static void Publish(TEvent eventData)
        {
            Listener[] currentListeners = listeners.ToArray();
            for (int i = 0; i < currentListeners.Length; i++)
            {
                currentListeners[i].Action(eventData);
            }
        }

        private static int CompareListeners(Listener left, Listener right)
        {
            int priorityResult = left.Priority.CompareTo(right.Priority);
            return priorityResult != 0
                ? priorityResult
                : left.Order.CompareTo(right.Order);
        }
    }
}
