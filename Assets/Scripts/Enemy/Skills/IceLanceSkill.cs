using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Ice Lance Skill", menuName = "ScriptableObject/Skills/Ice Lance")]
public class IceLanceSkill : SkillScriptableObject
{
    public int LancesToShoot = 2;
    public float Delay = 0.25f;
    public float Range = 10;
    public float BulletSpeed = 10;
    public PoolableObject Prefab;
    public LayerMask LineOfSightLayers;
    public float ProjectileRadius = 0.25f;
    public Vector3 BulletSpawnOffset = new Vector3(0, 1, 0);

    public override SkillScriptableObject ScaleUpForLevel(ScalingScriptableObject Scaling, int Level)
    {
        IceLanceSkill scaledUpSkill = CreateInstance<IceLanceSkill>();

        ScaleUpBaseValuesForLevel(scaledUpSkill, Scaling, Level);
        scaledUpSkill.LancesToShoot = LancesToShoot;
        scaledUpSkill.Delay = Delay;
        scaledUpSkill.Range = Range;
        scaledUpSkill.Prefab = Prefab;
        scaledUpSkill.LineOfSightLayers = LineOfSightLayers;
        scaledUpSkill.ProjectileRadius = ProjectileRadius;
        scaledUpSkill.BulletSpawnOffset = BulletSpawnOffset;

        return scaledUpSkill;
    }

    public override bool CanUseSkill(Enemy Enemy, Player Player, int Level)
    {
        return base.CanUseSkill(Enemy, Player, Level)
            && Vector3.Distance(Player.transform.position, Enemy.transform.position) <= Range
            && HasLineOfSightTo(Enemy, Player.transform);
    }

    private bool HasLineOfSightTo(Enemy Enemy, Transform Target)
    {
        if (Physics.SphereCast(
            Enemy.transform.position + BulletSpawnOffset,
            ProjectileRadius,
            ((Target.position + BulletSpawnOffset) - (Enemy.transform.position + BulletSpawnOffset)).normalized,
            out RaycastHit Hit,
            Range,
            LineOfSightLayers))
        {
            IDamageable damageable;

            if (Hit.collider.TryGetComponent<IDamageable>(out damageable))
            {
                return damageable.GetTransform() == Target;
            }
        }

        return false;
    }

    public override void UseSkill(Enemy Enemy, Player Player)
    {
        base.UseSkill(Enemy, Player);

        Enemy.StartCoroutine(ShootIceLances(Enemy, Player));
    }

    private IEnumerator ShootIceLances(Enemy Enemy, Player Player)
    {
        WaitForSeconds Wait = new WaitForSeconds(Delay);

        for (int i = 0; i < LancesToShoot; i++)
        {
            Enemy.Animator.SetTrigger(Enemy.ATTACK_TRIGGER);
            ShootIceLance(Enemy, Player);
            yield return Wait;
        }

        UseTime = Time.time;
        IsActivating = false;
    }

    private void ShootIceLance(Enemy Enemy, Player Player)
    {
        ObjectPool pool = ObjectPool.CreateInstance(Prefab, 5);

        PoolableObject instance = pool.GetObject();

        instance.transform.SetParent(Enemy.transform, false);
        instance.transform.localPosition = BulletSpawnOffset;
        instance.transform.rotation = Enemy.Agent.transform.rotation;

        Bullet bullet = instance.GetComponent<Bullet>();
        bullet.MoveSpeed = BulletSpeed;
        bullet.Spawn(Enemy.transform.forward, Damage, Player.transform);
    }
}
