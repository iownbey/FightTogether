using Hkmp.Api.Server;
using HkmpPouch;
using FightTogether.Events;
using System;
using System.Collections.Generic;

namespace FightTogether
{
    internal class Server : ServerAddon
    {
        public override bool NeedsNetwork => false;

        protected override string Name => Constants.AddonName;

        protected override string Version => Constants.AddonVersion;

        private PipeServer pipe;
        private IServerApi myServerApi;

        private readonly Dictionary<string, int> SceneEnemyHealthPool = [];
        private readonly Dictionary<int, List<string>> PlayerToEnemyPools = [];
        private readonly Dictionary<string, int> PlayerContributionToPool = [];

        private readonly char[] separator = ['|'];

        private string[] SplitData(PipeEvent e)
        {
            return e.EventData.Split(separator);
        }

        public override void Initialize(IServerApi serverApi)
        {
            pipe = new PipeServer(this.Name);
            myServerApi = serverApi;
            myServerApi.ServerManager.PlayerConnectEvent += ServerManager_PlayerConnectEvent;
            myServerApi.ServerManager.PlayerDisconnectEvent += ServerManager_PlayerDisconnectEvent;

            pipe.On(JoinPoolEventFactory.Instance).Do<JoinPoolEvent>((pipeEvent) =>
            {
                JoinPool(pipeEvent.FromPlayer, pipeEvent.BossName, pipeEvent.WithHealth);
            });
            pipe.On(ModifyPoolHealthEventFactory.Instance).Do<ModifyPoolHealthEvent>((pipeEvent) =>
            {
                ModifyPool(pipeEvent.BossName, pipeEvent.ReduceHealthBy);
            });
            pipe.On(LeavePoolEventFactory.Instance).Do<LeavePoolEvent>((pipeEvent) =>
            {
                LeavePool(pipeEvent.FromPlayer, pipeEvent.EventData);
            });
        }
        private void LeaveAllPools(int playerId)
        {
            if (PlayerToEnemyPools.TryGetValue(playerId, out var value2))
            {
                foreach (var v in value2)
                {
                    if (PlayerContributionToPool.TryGetValue(v, out var poolContribution))
                    {
                        SceneEnemyHealthPool[v] -= poolContribution;
                        if (SceneEnemyHealthPool[v] <= 0)
                        {
                            SceneEnemyHealthPool[v] = 0;
                        }
                        pipe.Broadcast(new PoolUpdateEvent { BossName = v, CurrentHealth = SceneEnemyHealthPool[v] });
                    }
                }
                value2.Clear();
            }
        }
        private void LeavePool(int playerId, string PoolName)
        {
            if (PlayerContributionToPool.TryGetValue(PoolName + playerId, out var value))
            {
                SceneEnemyHealthPool[PoolName] -= value;
                if (SceneEnemyHealthPool[PoolName] <= 0)
                {
                    SceneEnemyHealthPool[PoolName] = 0;
                }
                pipe.Broadcast(new PoolUpdateEvent { BossName = PoolName, CurrentHealth = SceneEnemyHealthPool[PoolName] });

            }
            if (PlayerToEnemyPools.TryGetValue(playerId, out var value2))
            {
                value2.Remove(PoolName);
            }
        }

        private void JoinPool(int playerId, string enemyName, int hp)
        {
            if (!SceneEnemyHealthPool.TryGetValue(enemyName, out var value))
            {
                SceneEnemyHealthPool[enemyName] = 0;
            }
            if (!PlayerToEnemyPools.TryGetValue(playerId, out var value2))
            {
                PlayerToEnemyPools[playerId] = new List<string>();
            }
            if (!PlayerToEnemyPools[playerId].Contains(enemyName))
            {
                PlayerToEnemyPools[playerId].Add(enemyName);
            }
            PlayerContributionToPool[enemyName + playerId.ToString()] = hp;
            SceneEnemyHealthPool[enemyName] += hp;
            if (SceneEnemyHealthPool[enemyName] <= 0)
            {
                SceneEnemyHealthPool[enemyName] = 0;
            }
            pipe.Broadcast(new PoolUpdateEvent { BossName = enemyName, CurrentHealth = SceneEnemyHealthPool[enemyName] });
        }

        private void ModifyPool(string enemyName, int damage)
        {

            if (!SceneEnemyHealthPool.TryGetValue(enemyName, out var value))
            {
                SceneEnemyHealthPool[enemyName] = 0;
            }
            SceneEnemyHealthPool[enemyName] -= damage;
            if (SceneEnemyHealthPool[enemyName] <= 0)
            {
                SceneEnemyHealthPool[enemyName] = 0;
            }
            pipe.Broadcast(new PoolUpdateEvent { BossName = enemyName, CurrentHealth = SceneEnemyHealthPool[enemyName] });
        }

        private void ServerManager_PlayerDisconnectEvent(IServerPlayer player)
        {
            LeaveAllPools(player.Id);
        }


        private void ServerManager_PlayerConnectEvent(IServerPlayer obj)
        {
            myServerApi.ServerManager.BroadcastMessage($"Another realm is brought into the tangle, {obj.Username} commits to the linking of fates");
        }
    }
}
