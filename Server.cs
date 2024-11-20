using Hkmp.Api.Server;
using HkmpPouch;
using FightTogether.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FightTogether
{
    class EnemyData
    {
        // Amount of health that is considered the starting amount.
        // Calculated based on the number of players
        public int startingHealth;

        // Amount of health each version of the monster has.
        // Once these add up to equal starting Health, the monster dies.
        public Dictionary<ushort, int> clientHealths = [];

        public int GetCurrentHealth()
        {
            return Math.Max(0, (startingHealth * Server.playerCount) - clientHealths.Values.Select(a => startingHealth - a).Sum());
        }
    }

    internal class Server : ServerAddon
    {
        public override bool NeedsNetwork => false;

        protected override string Name => Constants.AddonName;

        protected override string Version => Constants.AddonVersion;

        private PipeServer pipe;

        private readonly Dictionary<string, EnemyData> enemyData = [];

        internal static int playerCount;

        public override void Initialize(IServerApi serverApi)
        {
            pipe = new PipeServer(Name);
            ServerApi.ServerManager.PlayerConnectEvent += ServerManager_PlayerConnectEvent;
            ServerApi.ServerManager.PlayerDisconnectEvent += ServerManager_PlayerDisconnectEvent;

            pipe.On(UpdateHealthEventFactory.Instance).Do<HealthEvent>((pipeEvent) =>
            {
                switch (pipeEvent.operation)
                {
                    case HealthOperation.Init: InitHealth(pipeEvent.FromPlayer, pipeEvent.entityName, pipeEvent.health); break;
                    case HealthOperation.Update: UpdateHealth(pipeEvent.FromPlayer, pipeEvent.entityName, pipeEvent.health); break;
                }

            });
        }

        void InitHealth(ushort playerId, string entityName, int health)
        {
            if (!enemyData.ContainsKey(entityName))
            {
                enemyData[entityName] = new()
                {
                    startingHealth = health
                };

                EnemyData enemy = enemyData[entityName];
                enemy.clientHealths[playerId] = health;

                pipe.Broadcast(new HealthEvent(HealthOperation.Update, entityName, enemy.GetCurrentHealth()));
            }
        }

        void UpdateHealth(ushort playerId, string entityName, int health)
        {
            ServerApi.ServerManager.BroadcastMessage($"from {playerId}: {entityName} has {health} hp");

            EnemyData enemy = enemyData[entityName];
            enemy.clientHealths[playerId] = health;

            pipe.Broadcast(new HealthEvent(HealthOperation.Update, entityName, enemy.GetCurrentHealth()));
        }

        void ServerManager_PlayerDisconnectEvent(IServerPlayer player)
        {
            playerCount--;
            foreach (KeyValuePair<string, EnemyData> kvp in enemyData)
            {
                kvp.Value.clientHealths.Remove(player.Id);
                pipe.Broadcast(new HealthEvent(HealthOperation.Update, kvp.Key, kvp.Value.GetCurrentHealth()));
            }
        }

        void ServerManager_PlayerConnectEvent(IServerPlayer obj)
        {
            playerCount++;
            ServerApi.ServerManager.BroadcastMessage($"{obj.Username} wants to fight together");
            foreach (KeyValuePair<string, EnemyData> kvp in enemyData)
            {
                pipe.Broadcast(new HealthEvent(HealthOperation.Update, kvp.Key, kvp.Value.GetCurrentHealth()));
            }
        }
    }
}
