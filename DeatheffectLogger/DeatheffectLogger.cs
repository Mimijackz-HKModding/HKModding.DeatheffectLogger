using Modding;
using System;
using System.Reflection;
using System.IO;

namespace DeatheffectLogger
{
    public class DeatheffectLogger : Mod
    {
        private static DeatheffectLogger? _instance;

        private string newLogs = "";
        string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/output.txt";
        internal static DeatheffectLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException($"{nameof(DeatheffectLogger)} was never initialized");
                }
                return _instance;
            }
        }

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        public DeatheffectLogger() : base()
        {
            _instance = this;
        }

        // if you need preloads, you will need to implement GetPreloadNames and use the other signature of Initialize.
        public override void Initialize()
        {
            Log("Initializing");

            ModHooks.SavegameSaveHook += SaveLogs;
            On.EnemyDeathEffects.Start += EnemyDeathEffects_Start;
            ModHooks.OnEnableEnemyHook += ModHooks_OnEnableEnemyHook;
            // put additional initialization logic here

            Log("Initialized");
        }

        private bool ModHooks_OnEnableEnemyHook(UnityEngine.GameObject enemy, bool isAlreadyDead)
        {
            if (enemy.GetComponent<EnemyDeathEffects>() == null) return isAlreadyDead;
            EnemyDeathEffects self = enemy.GetComponent<EnemyDeathEffects>();
            var deathType = GetPrivateFieldValue<EnemyDeathTypes>(self, "enemyDeathType");
            newLogs += self.name + "," + deathType.ToString() + "\n";
            return isAlreadyDead;
        }

        private void EnemyDeathEffects_Start(On.EnemyDeathEffects.orig_Start orig, EnemyDeathEffects self)
        {
            orig(self);
            var deathType = GetPrivateFieldValue<EnemyDeathTypes>(self, "enemyDeathType");
            newLogs += self.name + "," + deathType.ToString() + "\n";

        }


        private void SaveLogs(int obj)
        {
            string origString = "";
            if (File.Exists(path)) origString = File.ReadAllText(path);
            Log("Saving new death effect logs");
            LogDebug(newLogs);
            File.WriteAllText(path, origString + newLogs);
            newLogs = "";
        }

        /// <summary>
        /// Returns a private Property Value from a given Object. Uses Reflection.
        /// Throws a ArgumentOutOfRangeException if the Property is not found.
        /// </summary>
        /// <typeparam name="T">Type of the Property</typeparam>
        /// <param name="obj">Object from where the Property Value is returned</param>
        /// <param name="propName">Propertyname as string.</param>
        /// <returns>PropertyValue</returns>
        public static T GetPrivateFieldValue<T>(object obj, string propName)//yes, it is copy-pasted
        {
            if (obj == null) throw new ArgumentNullException("obj");
            Type t = obj.GetType();
            FieldInfo fi = null;
            while (fi == null && t != null)
            {
                fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                t = t.BaseType;
            }
            if (fi == null) throw new ArgumentOutOfRangeException("propName", string.Format("Field {0} was not found in Type {1}", propName, obj.GetType().FullName));
            return (T)fi.GetValue(obj);
        }
    }
}
