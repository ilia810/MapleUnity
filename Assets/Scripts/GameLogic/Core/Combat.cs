using System;
using System.Collections.Generic;
using System.Linq;

namespace MapleClient.GameLogic.Core
{
    public class Combat
    {
        private const float AttackCooldown = 0.6f; // 600ms between attacks
        private readonly Dictionary<Player, float> lastAttackTime = new Dictionary<Player, float>();
        private float currentTime = 0;

        public event Action<Player, Monster, int> DamageDealt;

        public void Update(float deltaTime)
        {
            currentTime += deltaTime;
        }

        public bool CanPlayerAttack(Player player)
        {
            if (!lastAttackTime.TryGetValue(player, out float lastTime))
                return true;

            return (currentTime - lastTime) >= AttackCooldown;
        }

        public List<Monster> PerformBasicAttack(Player player, List<Monster> monsters, float range)
        {
            var hitMonsters = new List<Monster>();

            if (!CanPlayerAttack(player))
                return hitMonsters;

            // Record attack time
            lastAttackTime[player] = currentTime;

            // Find monsters in range
            var targetsInRange = monsters
                .Where(m => !m.IsDead)
                .Where(m => IsInAttackRange(player, m, range))
                .ToList();

            // Apply damage to each target
            foreach (var monster in targetsInRange)
            {
                var damage = CalculatePhysicalDamage(player, monster);
                monster.TakeDamage(damage);
                hitMonsters.Add(monster);
                
                DamageDealt?.Invoke(player, monster, damage);
            }

            return hitMonsters;
        }

        public int CalculatePhysicalDamage(Player player, Monster monster)
        {
            // Simple damage formula for now
            var baseDamage = player.GetBaseDamage();
            var defense = monster.Template?.PhysicalDefense ?? 0;
            
            // Add some variance (±20%)
            var random = new Random();
            var variance = 0.8f + (float)(random.NextDouble() * 0.4f);
            
            var damage = Math.Max(1, (int)((baseDamage - defense) * variance));
            return damage;
        }

        private bool IsInAttackRange(Player player, Monster monster, float range)
        {
            var distance = Math.Abs(player.Position.X - monster.Position.X);
            
            // Also check Y distance (must be on similar height)
            var yDistance = Math.Abs(player.Position.Y - monster.Position.Y);
            
            return distance <= range && yDistance <= 50; // 50 pixel vertical tolerance
        }
    }
}