
public enum ServiceStatus
    {
        Initializing,
        Initialized,
        Starting,
        Started,
        Stoping,
        Stoped,
        Paused,
        Executing,
        Terminated,
    }

    public interface IServicePluginBase
    {
        string Name { get; }
        int Delay { get; }
        bool IsEligibleToRun { get; }
        bool IsCompleted { get; set; }
        bool IsTerminated { get; set; }
        bool IsRestartRequired { get; set; }
        ServiceStatus Status { get; set; }
        IDatabaseInfo Database { get; set; }
        void Init();
        void Start();
        void Execute();
        void Stop();
        void ReStart();
        void Wait();
    }



    public interface IServiceLogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message, Exception ex);
        void Fatal(string message, Exception ex);
    }
    
    public class ServiceFirstImpl
    {
        private bool IsTerminate { get; set; }
        public delegate void UpdateStatusDelegate(string message);
        public ServiceFirstImpl()
        {
            ExecutablePluginList = new List<DatabaseInfo>();
        }
        private List<DatabaseInfo> ExecutablePluginList { get; set; }
        public event UpdateStatusDelegate OnUpdateStatus;
        private void UpdateStatus(string message)
        {
            if (OnUpdateStatus != null)
            {
                OnUpdateStatus(string.Format("{0}{1}{2}", DateTime.Now, message, Environment.NewLine));
            }
        }
        public void Init()
        {
            ExecutablePluginList.ForEach(db =>
            {
                UpdateStatus(string.Format("Db: {0} - Plugin: {1} Initializing.", db.Database, db.Plugin.Name));
                Task.Factory.StartNew(db.Plugin.Init);
                UpdateStatus(string.Format("Db: {0} - Plugin: {1} Initialization Completed.", db.Database, db.Plugin.Name));
            });
        }

        public void Stop()
        {
            IsTerminate = true;
        }

        public void Start()
        {
            StartAllServices();
            MonitorServices();
        }

        public void Stop(string database, string pluginName)
        {
            var pluginInfo =
                ExecutablePluginList.FirstOrDefault(
                    db =>
                        db.Database.Equals(database, StringComparison.CurrentCultureIgnoreCase) &&
                        db.Plugin.Name.Equals(pluginName, StringComparison.CurrentCultureIgnoreCase));
            if (pluginInfo != null)
            {
                pluginInfo.Plugin.IsTerminated = true;
            }

        }

        private void MonitorServices()
        {
            while (true)
            {
                Wait(60000); //1 Min
                if (IsTerminate)
                {
                    StopAllServices();
                    break;
                }
                StartStopedService();
            }
        }
        private static void Wait(int milliSeconds)
        {
            Thread.Sleep(milliSeconds);
        }
        private void StartServices(IEnumerable<DatabaseInfo> pluginList)
        {
            var tasks = pluginList.Select(db =>
            {
                UpdateStatus(string.Format("Db: {0} - Plugin: {1} Starting.", db.Database, db.Plugin.Name));
                var task = Task.Factory.StartNew(db.Plugin.Start);
                UpdateStatus(string.Format("Db: {0} - Plugin: {1} Started.", db.Database, db.Plugin.Name));
                return task;
            }).ToArray();
            //Task.WaitAll(tasks); No need to wait for all thread to complete.
        }

        private void StartStopedService()
        {
            var stopedServices = ExecutablePluginList.Where(x => x.Plugin.Status == ServiceStatus.Stoped);
            StartServices(stopedServices);
        }

        internal void InitAllServices()
        {
            var tasks = ExecutablePluginList
                .Where(x => x.Plugin.Status != ServiceStatus.Initialized)
                .Select(db =>
             {
                 UpdateStatus(string.Format("Db: {0} - Plugin: {1} Initializing.", db.Database, db.Plugin.Name));
                 var task = Task.Factory.StartNew(db.Plugin.Start);
                 UpdateStatus(string.Format("Db: {0} - Plugin: {1} Started.", db.Database, db.Plugin.Name));
                 return task;
             }).ToArray();
            Task.WaitAll(tasks);
        }

        internal void StartAllServices()
        {
            StartServices(ExecutablePluginList);
        }
        internal void StopAllServices()
        {
            ExecutablePluginList.ForEach(x => x.Plugin.Stop());
            while (ExecutablePluginList.Any(x => !x.Plugin.IsCompleted))
            {
                Wait(5000); // 5 Seconds.
            }
            try
            {

            }
            catch (AggregateException aex)
            {
                aex.Flatten();
                foreach (var ex in aex.InnerExceptions)
                {
                    
                }
            }
        }
        internal void RestartAllServices()
        {
            StopAllServices();
            StartAllServices();
        }
    }
    
    public abstract class ServicePluginBase : IServicePluginBase
    {
        public bool IsCompleted { get; set; }
        public bool IsTerminated { get; set; }
        public virtual void Init() { }
        public void Execute()
        {
            if (!IsTerminated)
            {
                IsCompleted = false;
                ExecutePlugin();
                IsCompleted = true;
            }
        }
        internal abstract void ExecutePlugin();
        public virtual int Delay
        {
            get { return 0; }
        }
        public virtual bool IsEligibleToRun
        {
            get { return true; }
        }
        public abstract string Name { get; }
        public abstract void Start();

        public void Stop()
        {

        }

        public virtual void ReStart()
        {
            Stop();
            Start();
        }
        public virtual void Wait()
        {
            Thread.Sleep(Delay);
        }

        //internal void Wait(int milliSeconds)
        //{
        //    Thread.Sleep(milliSeconds);
        //}


        public bool IsRestartRequired
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }

        public ServiceStatus Status
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }


        public IDatabaseInfo Database
        {
            get
            {
                throw new System.NotImplementedException();
            }
            set
            {
                throw new System.NotImplementedException();
            }
        }
    }