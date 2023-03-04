using Hkmp.Api.Server;
using HkmpPouch;
using ESoulLink.Events;
using System;
using System.Collections.Generic;

namespace ESoulLink
{
    internal class Server : ServerAddon
    {
        public override bool NeedsNetwork => false;

        protected override string Name => Constants.AddonName;

        protected override string Version => Constants.AddonVersion;

        private PipeServer pipe;
        private IServerApi myServerApi;

        private Dictionary<string, int> SceneEnemyHealthPool = new();
        private Dictionary<int, List<string>> PlayerToEnemyPools = new();
        private Dictionary<string, int> PlayerContributionToPool = new();

            private char[] separator = new char[] { '|' };

        private string[] SplitData(PipeEvent e)
        {
            return e.EventData.Split(separator);
        }

        public override void Initialize(IServerApi serverApi)
        {
            pipe = new PipeServer(this.Name);
            myServerApi = serverApi;
            pipe.ServerApi.ServerManager.PlayerConnectEvent += ServerManager_PlayerConnectEvent;
            pipe.ServerApi.ServerManager.PlayerDisconnectEvent += ServerManager_PlayerDisconnectEvent;

            pipe.On(JoinPoolEventFactory.Instance).Do<JoinPoolEvent>((pipeEvent) =>{
                Logger.Info("JoinPool Event has been made to work!");
                JoinPool(pipeEvent.FromPlayer, pipeEvent.BossName, pipeEvent.WithHealth);
            });
            pipe.On(ModifyPoolHealthEventFactory.Instance).Do<ModifyPoolHealthEvent>((pipeEvent) => {
                ModifyPool(pipeEvent.BossName,pipeEvent.ReduceHealthBy);
            });
            pipe.On(LeavePoolEventFactory.Instance).Do<LeavePoolEvent>((pipeEvent) => {
                LeavePool(pipeEvent.FromPlayer, pipeEvent.EventData);
            });
        }
        private void LeaveAllPools(int playerId) {
            if (PlayerToEnemyPools.TryGetValue(playerId, out var value2))
            {
                foreach (var v in value2)
                {
                    try
                    {
                        SceneEnemyHealthPool[v] -= PlayerContributionToPool[v + playerId.ToString()];
                        if (SceneEnemyHealthPool[v] <= 0)
                        {
                            SceneEnemyHealthPool[v] = 0;
                        }
                        pipe.Broadcast( new PoolUpdateEvent {BossName = v, CurrentHealth = SceneEnemyHealthPool[v]});
                    }
                    catch (Exception e)
                    {
                        pipe.Logger.Error(e.ToString());
                    }
                }
                value2.Clear();
            }
        }
        private void LeavePool(int playerId, string PoolName)
        {

            pipe.Logger.Debug($"LeavePool {playerId}");
            try
            {
                SceneEnemyHealthPool[PoolName] -= PlayerContributionToPool[PoolName + playerId.ToString()];
                if (SceneEnemyHealthPool[PoolName] <= 0)
                {
                    SceneEnemyHealthPool[PoolName] = 0;
                }
                pipe.Broadcast(new PoolUpdateEvent { BossName = PoolName, CurrentHealth = SceneEnemyHealthPool[PoolName] });
            }
            catch (Exception e)
            {
                pipe.Logger.Error(e.ToString());
            }
            if (PlayerToEnemyPools.TryGetValue(playerId, out var value2))
            {
                value2.Remove(PoolName);
            }
        }

        private void JoinPool(int playerId,string enemyName,int hp)
        {
            pipe.Logger.Debug($"Join pool {playerId} {enemyName} {hp}");
            if (!SceneEnemyHealthPool.TryGetValue(enemyName, out var value))
            {
                SceneEnemyHealthPool[enemyName] = 0;
            }
            if (!PlayerToEnemyPools.TryGetValue(playerId, out var value2))
            {
                PlayerToEnemyPools[playerId] = new List<string>();
            }
            if (!PlayerToEnemyPools[playerId].Contains(enemyName)) { 
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

        private void ModifyPool(string enemyName,int damage)
        {

            pipe.Logger.Debug($"ModifyPool {enemyName} {damage}");
            if (!SceneEnemyHealthPool.TryGetValue(enemyName, out var value))
            {
                SceneEnemyHealthPool[enemyName] = 0;
            }
            SceneEnemyHealthPool[enemyName] -= damage;
            if(SceneEnemyHealthPool[enemyName] <= 0)
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
            myServerApi.ServerManager.BroadcastMessage($"Player {obj.Username} Has joined with the shared enemy health pool");
        }
    }
}
