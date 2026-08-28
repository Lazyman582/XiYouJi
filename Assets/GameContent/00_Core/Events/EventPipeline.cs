using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace XiYouJi.Events
{
    /// <summary>
    /// 所有可以通过 EventPipeline 发布的事件都实现这个标记接口。
    /// 事件对象建议只保存本次事件所需的数据，不保存处理逻辑。
    /// </summary>
    public interface IGameEvent
    {
    }

    /// <summary>
    /// 一次事件发布的上下文。除了事件本身，还可以在这里传递临时数据。
    /// </summary>
    public sealed class EventContext<TEvent> where TEvent : IGameEvent
    {
        private readonly Dictionary<string, object> data = new Dictionary<string, object>();

        internal EventContext(TEvent eventData, object source)
        {
            Event = eventData;
            Source = source;
            CreatedAtUtc = DateTime.UtcNow;
        }

        public TEvent Event { get; private set; }
        public object Source { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public bool IsStopped { get; private set; }
        public bool IsCanceled { get; private set; }
        public string CancelReason { get; private set; }
        public IReadOnlyDictionary<string, object> Data { get { return data; } }

        /// <summary>
        /// 停止后续阶段，但这次事件仍然算成功完成。
        /// </summary>
        public void StopPropagation()
        {
            IsStopped = true;
        }

        /// <summary>
        /// 取消事件并停止后续阶段。发布报告的 IsCanceled 会变为 true。
        /// </summary>
        public void Cancel(string reason = null)
        {
            IsCanceled = true;
            IsStopped = true;
            CancelReason = reason;
        }

        public void SetData<TValue>(string key, TValue value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Event context data key cannot be empty.", "key");
            }

            data[key] = value;
        }

        public bool TryGetData<TValue>(string key, out TValue value)
        {
            object rawValue;
            if (data.TryGetValue(key, out rawValue) && rawValue is TValue)
            {
                value = (TValue)rawValue;
                return true;
            }

            value = default(TValue);
            return false;
        }
    }

    public interface IEventHandler<TEvent> where TEvent : IGameEvent
    {
        void Handle(EventContext<TEvent> context);
    }

    public interface IEventSubscription : IDisposable
    {
        bool IsDisposed { get; }
    }

    /// <summary>
    /// 一次 Publish 的执行结果，适合用于日志、调试或需要知道事件是否被取消的调用方。
    /// </summary>
    public sealed class EventExecutionReport
    {
        internal EventExecutionReport(Type eventType, object eventData)
        {
            EventType = eventType;
            Event = eventData;
            Succeeded = true;
        }

        public Type EventType { get; private set; }
        public object Event { get; private set; }
        public int RegisteredStageCount { get; internal set; }
        public int InvokedStageCount { get; internal set; }
        public bool Succeeded { get; internal set; }
        public bool IsStopped { get; internal set; }
        public bool IsCanceled { get; internal set; }
        public string CancelReason { get; internal set; }
        public Exception Exception { get; internal set; }
        public long ElapsedMilliseconds { get; internal set; }
    }

    /// <summary>
    /// 简易同步事件管线。
    ///
    /// 同一个事件类型可以注册多个处理阶段，order 越小越早执行；order 相同时保持注册顺序。
    /// </summary>
    public sealed class EventPipeline
    {
        private readonly Dictionary<Type, List<IEventStage>> stagesByEventType =
            new Dictionary<Type, List<IEventStage>>();
        private readonly bool stopOnException;
        private readonly Action<Exception, Type, string> exceptionHandler;
        private long nextRegistrationSequence;
        private bool isDisposed;

        public EventPipeline(
            bool stopOnException = true,
            Action<Exception, Type, string> exceptionHandler = null)
        {
            this.stopOnException = stopOnException;
            this.exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// 注册一个事件处理阶段。返回的订阅对象应在 OnDisable/OnDestroy 中 Dispose。
        /// </summary>
        public IEventSubscription Subscribe<TEvent>(
            Action<EventContext<TEvent>> handler,
            int order = 0,
            string name = null) where TEvent : IGameEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            ThrowIfDisposed();

            DelegateEventStage<TEvent> stage = new DelegateEventStage<TEvent>(
                handler,
                order,
                nextRegistrationSequence++,
                string.IsNullOrEmpty(name) ? handler.Method.Name : name);

            List<IEventStage> stages;
            if (!stagesByEventType.TryGetValue(typeof(TEvent), out stages))
            {
                stages = new List<IEventStage>();
                stagesByEventType.Add(typeof(TEvent), stages);
            }

            stages.Add(stage);
            stages.Sort(CompareStages);
            return new EventSubscription(delegate { RemoveStage(stage); });
        }

        public IEventSubscription Subscribe<TEvent>(
            IEventHandler<TEvent> handler,
            int order = 0,
            string name = null) where TEvent : IGameEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }

            return Subscribe<TEvent>(handler.Handle, order, name ?? handler.GetType().Name);
        }

        /// <summary>
        /// 顺序执行当前事件类型的所有处理阶段。
        /// </summary>
        public EventExecutionReport Publish<TEvent>(TEvent eventData, object source = null)
            where TEvent : IGameEvent
        {
            if (eventData == null)
            {
                throw new ArgumentNullException("eventData");
            }

            ThrowIfDisposed();

            EventContext<TEvent> context = new EventContext<TEvent>(eventData, source);
            EventExecutionReport report = new EventExecutionReport(typeof(TEvent), eventData);
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<IEventStage> registeredStages;
            if (!stagesByEventType.TryGetValue(typeof(TEvent), out registeredStages))
            {
                stopwatch.Stop();
                report.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return report;
            }

            // 复制一份，允许处理阶段在执行期间订阅或取消订阅，不影响本次遍历。
            IEventStage[] stages = registeredStages.ToArray();
            report.RegisteredStageCount = stages.Length;

            for (int i = 0; i < stages.Length; i++)
            {
                if (context.IsStopped || context.IsCanceled)
                {
                    break;
                }

                IEventStage stage = stages[i];
                report.InvokedStageCount++;
                try
                {
                    stage.Invoke(context);
                }
                catch (Exception exception)
                {
                    report.Succeeded = false;
                    report.Exception = exception;
                    ReportException(exception, typeof(TEvent), stage.Name);
                    if (stopOnException)
                    {
                        break;
                    }
                }
            }

            stopwatch.Stop();
            report.IsStopped = context.IsStopped;
            report.IsCanceled = context.IsCanceled;
            report.CancelReason = context.CancelReason;
            report.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            return report;
        }

        /// <summary>
        /// 清空所有订阅。适合测试或切换场景时使用。
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            stagesByEventType.Clear();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            stagesByEventType.Clear();
        }

        private void RemoveStage(IEventStage stage)
        {
            if (isDisposed)
            {
                return;
            }

            List<IEventStage> stages;
            if (!stagesByEventType.TryGetValue(stage.EventType, out stages))
            {
                return;
            }

            stages.Remove(stage);
            if (stages.Count == 0)
            {
                stagesByEventType.Remove(stage.EventType);
            }
        }

        private void ReportException(Exception exception, Type eventType, string stageName)
        {
            if (exceptionHandler == null)
            {
                return;
            }

            try
            {
                exceptionHandler(exception, eventType, stageName);
            }
            catch
            {
                // 错误回调不能反过来破坏事件管线。
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException("EventPipeline");
            }
        }

        private static int CompareStages(IEventStage left, IEventStage right)
        {
            int orderComparison = left.Order.CompareTo(right.Order);
            return orderComparison != 0
                ? orderComparison
                : left.RegistrationSequence.CompareTo(right.RegistrationSequence);
        }

        private interface IEventStage
        {
            Type EventType { get; }
            int Order { get; }
            long RegistrationSequence { get; }
            string Name { get; }
            void Invoke(object context);
        }

        private sealed class DelegateEventStage<TEvent> : IEventStage where TEvent : IGameEvent
        {
            private readonly Action<EventContext<TEvent>> handler;

            public DelegateEventStage(
                Action<EventContext<TEvent>> handler,
                int order,
                long registrationSequence,
                string name)
            {
                this.handler = handler;
                Order = order;
                RegistrationSequence = registrationSequence;
                Name = name;
            }

            public Type EventType { get { return typeof(TEvent); } }
            public int Order { get; private set; }
            public long RegistrationSequence { get; private set; }
            public string Name { get; private set; }

            public void Invoke(object context)
            {
                handler((EventContext<TEvent>)context);
            }
        }

        private sealed class EventSubscription : IEventSubscription
        {
            private Action unsubscribe;

            public EventSubscription(Action unsubscribe)
            {
                this.unsubscribe = unsubscribe;
            }

            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                IsDisposed = true;
                Action action = unsubscribe;
                unsubscribe = null;
                if (action != null)
                {
                    action();
                }
            }
        }
    }

    /// <summary>
    /// 全局管线入口。小型项目可直接使用；大型系统也可以自行 new EventPipeline 做依赖注入。
    /// </summary>
    public static class GameEvents
    {
        private static readonly EventPipeline global = new EventPipeline();

        public static EventPipeline Global { get { return global; } }

        public static IEventSubscription Subscribe<TEvent>(
            Action<EventContext<TEvent>> handler,
            int order = 0,
            string name = null) where TEvent : IGameEvent
        {
            return global.Subscribe(handler, order, name);
        }

        public static EventExecutionReport Publish<TEvent>(TEvent eventData, object source = null)
            where TEvent : IGameEvent
        {
            return global.Publish(eventData, source);
        }

        public static void Clear()
        {
            global.Clear();
        }
    }
}
