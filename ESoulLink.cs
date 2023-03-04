using Hkmp.Api.Server;
using HkmpPouch;
using Modding;
using Satchel;
using System.Collections.Generic;
using UnityEngine;

namespace ESoulLink
{
    public class ESoulLink : Mod
    {
        internal static ESoulLink Instance;
        internal static Server server;
        internal static PipeClient pipeClient;

        internal static Dictionary<string, HealthManager> healthManagers = new Dictionary<string, HealthManager>();

        public override string GetVersion()
        {
            return Constants.AddonVersion; 
        }


        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {

            Instance = this;

            if (server == null)
            {
                server = new Server();
                ServerAddon.RegisterAddon(server);
            }
            if (pipeClient == null) {
                pipeClient = new PipeClient(Constants.AddonName);
            }
            pipeClient.ServerCounterPartAvailable((isServerAddonPresent) =>
            {
                if (isServerAddonPresent)
                {
                    pipeClient.ClientApi.UiManager.ChatBox.AddMessage("May the souls of your enemies be linked, the fates of your realms intertwined.");
                    On.HealthManager.Start += HealthManager_Start;
                    UnityEngine.SceneManagement.SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
                }
                else {
                    pipeClient.ClientApi.UiManager.ChatBox.AddMessage("This realm forbids the linking of enemy souls");
                }
            });

            
        }

        private void SceneManager_activeSceneChanged(UnityEngine.SceneManagement.Scene arg0, UnityEngine.SceneManagement.Scene arg1)
        {
            var gos = GameObjectUtils.GetAllGameObjectsInScene();
            foreach(var go in gos)
            {
                JoinPool(go);
            }
        }

        private void HealthManager_Start(On.HealthManager.orig_Start orig, HealthManager self)
        {
            orig(self);
            JoinPool(self.gameObject);
        }

        private void JoinPool(GameObject go)
        {
            var hm = go.GetComponent<HealthManager>();
            if (hm == null || hm.isDead)
            {
                return;
            }
            go.GetAddComponent<HpLinkBehaviour>();
        }

    }

}