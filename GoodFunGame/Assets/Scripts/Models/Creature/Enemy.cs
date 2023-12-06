
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static EnemySpawn;
using static UnityEngine.RuleTile.TilingRuleOutput;
using Random = UnityEngine.Random;

public class Enemy : Creature 
{
    #region Properties
    public EnemyData.EnemyKey enemyType;
    public int hp;
    public float speed;
    public int currentHp;
    public int damage;
    public EnemyData.FireType fireType;
    public Pattern movePattern;
    #endregion

    #region Fields

    public Coroutine MoveCoroutine;
    private Coroutine _coAttack;
    #endregion

    #region MonoBehaviours

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("PlayerProjectile"))
        {
            Projectile projectile = collision.gameObject.GetComponent<Projectile>();
            OnHit(projectile.Owner);

            Main.Object.Player.ScoreCount++;
        }
    }

    protected override void Update()
    {
        _coAttack ??= StartCoroutine(CoAttack());
    }

    #endregion

    #region Initialize / Set
    public override bool Initialize() 
    {
        if (base.Initialize() == false) return false;
        foreach (KeyValuePair<string, EnemyData> enemy in Main.Data.Enemies)
        {
            SetInfo(enemy.Key);
        }
        return true;
    }

    public override void SetInfo(string key) {
        base.SetInfo(key);

        EnemyData enemy = Main.Data.Enemies.FirstOrDefault(e => e.Key == key).Value;
        enemyType = (EnemyData.EnemyKey)Enum.Parse(typeof(EnemyData.EnemyKey), enemy.keyName);
        hp = enemy.hp;
        currentHp = hp;
        speed = enemy.speed;
        damage = enemy.damage;
        movePattern = AssignmentPattern(key);
    }

    /// <summary>
    ///  랜덤한 패턴 타입 할당 "Key == BOSS를 포함하면 BOSS Type을 반환"
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private Pattern AssignmentPattern(string key)
    {
        Pattern patternType;
        if (key.Contains("BOSS"))
        {
            patternType = Pattern.BOSS;
        }
        else
        {
            Pattern[] patternTypes = Enum.GetValues(typeof(Pattern)).Cast<Pattern>().ToArray();
            patternType = patternTypes[Random.Range(0,patternTypes.Length)];
        }
        return patternType;
    }
    #endregion

    #region State
    protected override void OnStateEntered_Dead() 
    {
        base.OnStateEntered_Dead();

        // 적을 죽일때마다 스코어 점수 추가
        Main.Object.Player.ScoreCount += 50;
        // 터지는 효과
        Main.Resource.InstantiatePrefab("Explosion.prefab", transform);

        // TODO:: 오브젝트 디스폰
        MoveCoroutine = StartCoroutine(ExplosionVFX());
        EndToEnemyCoroutine(this);
    }

    private IEnumerator ExplosionVFX()
    {
        // 터지는 효과
        var explosionVFX =Main.Object.Spawn<Thing>("Explosion", this.transform.position);
        yield return new WaitForSeconds(1f);
        // 터지는 효과 없애기
        EndToEnemyCoroutine(explosionVFX);
    }
    #endregion

    #region MoveMentPattern
    /// <summary>
    ///  지그재그패턴  총 3번의 움직임 변화가 있음  (파라매터로 전달)
    /// </summary>
    public void Zigzag()
    {
        CoroutineInit();
        Vector2[] wayPoints = Main.Spawn.CalculateWaypoints(this, 3);
        MoveCoroutine = StartCoroutine(Main.Spawn.MoveZigzag(this, wayPoints));
    }
    /// <summary>
    ///  일직선 움직임
    /// </summary>
    public void Vertical()
    {
        CoroutineInit();
        MoveCoroutine = StartCoroutine(Main.Spawn.MoveToVertical(this));
    }

    /// <summary>
    ///  보스일때 움직임
    /// </summary>
    public void Boss()
    {
        CoroutineInit();
        MoveCoroutine = StartCoroutine(Main.Spawn.BossAppear(this));
    }

    public void BossHorizontal()
    {
        CoroutineInit();
        MoveCoroutine = StartCoroutine(Main.Spawn.BossHorizontalPattern(this));
    }

    public void BossInfinity()
    {
        CoroutineInit();
        MoveCoroutine = StartCoroutine(Main.Spawn.BossInfinityPattern(this));
    }

    public void BossInAndOut()
    {
        CoroutineInit();
        MoveCoroutine = StartCoroutine(Main.Spawn.BossInAndOutPattern(this));
    }

    public void EndToEnemyCoroutine<T>(T coroutineObject) where T : Thing
    {
        CoroutineInit();
        Main.Object.Despawn(coroutineObject);
    }

    private void CoroutineInit()
    {
        if (MoveCoroutine == null) return;
        StopCoroutine(MoveCoroutine);
        MoveCoroutine = null;
    }
    #endregion

    #region AttackPattern

    private IEnumerator CoAttack()
    {
        PG_Fan fanShot = Main.Object.SpawnProjectileGenerator<PG_Fan>();
        fanShot.transform.position = this.transform.position;
        fanShot.transform.SetParent(this.transform);
        fanShot.Initialize(this, "Bullet_1_KSJ", 10, 6, 3, 250, 40);
        fanShot.Shot();
        yield return new WaitUntil(() => fanShot == null);
        yield return new WaitForSeconds(1);
        _coAttack = null;
        yield break;
    }

    #endregion
}