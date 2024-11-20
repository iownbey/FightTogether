using FightTogether.Events;
using Hkmp.Api.Server;
using HkmpPouch;
using Modding;
using Modding.Utils;
using Satchel;
using System.Collections.Generic;
using UnityEngine;

namespace FightTogether
{

    public class FightTogether : Mod
    {
        internal static FightTogether Instance;
        internal static Server server;
        internal static PipeClient pipeClient;

        internal static Dictionary<string, int> enemyHealths = [];

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
            pipeClient ??= new PipeClient(Constants.AddonName);
            pipeClient.ServerCounterPartAvailable((isServerAddonPresent) =>
            {
                if (isServerAddonPresent)
                {
                    pipeClient.ClientApi.UiManager.ChatBox.AddMessage("Fight Together is connected");
                    On.HealthManager.Start += HealthManager_Start;
                    pipeClient.On(UpdateHealthEventFactory.Instance).Do<HealthEvent>((e) =>
                    {
                        enemyHealths[e.entityName] = e.health;
                    });
                }
                else
                {
                    pipeClient.ClientApi.UiManager.ChatBox.AddMessage("Your server does not have Fight Together installed");
                }
            });
        }

        private void HealthManager_Start(On.HealthManager.orig_Start orig, HealthManager hm)
        {
            orig(hm);
            // don't add health sync to anonymously instantiated health managers
            if (hm.gameObject.name != "New GameObject")
            {
                hm.gameObject.GetOrAddComponent<HpLinkBehaviour>();
            };
        }
    }

}