using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "Poison Gas Skill", menuName = "ScriptableObject/Skills/Poison Gas")]
public class PoisonGasSkill : SkillScriptableObject
{
    public float Duration = 10f;
    public float TickRate = 0.3f;
    public float Range = 6;
    public PoolableObject Prefab;

    public override SkillScriptableObject ScaleUpForLevel(ScalingScriptableObject Scaling, int Level)
    {
        PoisonGasSkill scaledSkill = CreateInstance<PoisonGasSkill>();

        ScaleUpBaseValuesForLevel(scaledSkill, Scaling, Level);
        scaledSkill.Duration = Duration;
        scaledSkill.TickRate = TickRate;
        scaledSkill.Range = Range;
        scaledSkill.Prefab = Prefab;

        return scaledSkill;
    }

    public override bool CanUseSkill(Enemy Enemy, Player Player, int Level)
    {
        return base.CanUseSkill(Enemy, Player, Level)
            && Vector3.Distance(Enemy.transform.position, Player.transform.position) <= Range;
    }

    public override void UseSkill(Enemy Enemy, Player Player)
    {
        base.UseSkill(Enemy, Player);
        Enemy.StartCoroutine(SpawnPoisonGas(Enemy, Player));
    }

    private IEnumerator SpawnPoisonGas(Enemy Enemy, Player Player)
    {
        ObjectPool pool = ObjectPool.CreateInstance(Prefab, 5);

        PoolableObject instance = pool.GetObject();

        if (instance != null)
        {
            instance.transform.position = Player.transform.position;
            AreaDamage areaDamage = instance.GetComponentInChildren<AreaDamage>();

            areaDamage.Damage = Damage;
            areaDamage.TickRate = TickRate;
        }

        yield return new WaitForSeconds(Duration);

        instance.gameObject.SetActive(false);

        UseTime = Time.time;
        IsActivating = false;
    }
}
