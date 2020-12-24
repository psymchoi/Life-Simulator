﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.VFX;

public enum eLevel
{
    Low,
    Middle,
    High,

    None
}

public class AIManager : PlayerManager
{
    [Header("Ai Level")]
    [SerializeField] private eLevel m_level = eLevel.None;

    [Header("Ai Path")]
    [SerializeField] private List<Transform> m_aiPathTf;

    Sequence test;

    protected override void Awake()
    {
        base.Awake();

        m_aiPathTf = new List<Transform>();
    }


    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }


    #region (GameState = Ready) AI 시작위치 찾기
    /// <summary>
    /// (GameState = Ready)Ai 시작 위치 Transform 찾는 함수
    /// </summary>
    public override void SetPlayerStartPos()
    {
        m_playerStartTf = this.transform.position;
    }

    /// <summary>
    /// (GameState = Play/End)Ai 시작 위치로 초기화 함수
    /// </summary>
    protected override void SetPlayerToStartPos()
    {
        base.SetPlayerToStartPos();
    }
    #endregion


    #region AI 애니메이션 실행
    public override void SetAnim_Idle()
    {
        base.SetAnim_Idle();
    }
    /// <summary>
    /// (GameState = Play)Ai Push 애니메이션 실행함수
    /// </summary>
    public override void SetAnim_Push()
    {
        base.SetAnim_Push();
    }

    /// <summary>
    /// (GameState = Play)Ai Clear 애니메이션 실행함수
    /// </summary>
    public override void SetAnim_Clear()
    {
        // === ClearPos로 이동 === //
        playerClearSeq = DOTween.Sequence()
                                .Append(this.transform.DOMove(m_clearTf.position, 1.5f))
                                .Join(this.transform.DORotate(m_clearTf.eulerAngles, 1.5f))
                                .OnComplete(() =>
                                {
                                    m_playerState = ePlayerState.Happy;

                                    m_animCtrl.SetTrigger("Clear");
                                    m_splashEffect.SetActive(false);

                                    // === 눈덩이 Player Obj에서 떼어낸 후 => 굴러가게 === //
                                    m_snowBallObj.transform.SetParent(m_playerParentTf.transform);
                                    m_snowBallObj.transform.DORotate(Vector3.zero, 1f);
                                    // === 눈덩이 Player Obj에서 떼어낸 후 => 굴러가게 === //
                                });
        // === ClearPos로 이동 === //

        this.gameObject.SetActive(false);
    }


    /// <summary>
    /// (GameState = End)Ai GameOver 애니메이션 실행함수
    /// </summary>
    public void SetAiAnim_GameOver()
    {
        m_animCtrl.ResetTrigger("Push");
        m_animCtrl.SetTrigger("GameOver");                                // 플레이어 Die 애니메이션 실행


        m_snowBallMeshRdr.enabled = false;                                // 눈덩이 메쉬렌더러 끄기
        //m_characterMeshRdr.enabled = false;                          // 캐릭터 렌더러 끄기


        m_splashEffect.SetActive(false);                                  // 플레이어 달리는 이펙트 끄기


        StopPlayerMoving();                                               // 플레이어 움직임 Stop


        playerGameOverSeq = DOTween.Sequence()
                       .AppendInterval(2.5f)
                       .AppendCallback(() =>
                       {
                           this.transform.DOKill();

                           SetAnim_Idle();
                           SetPlayerToStartPos();                         // 플레이어 초기 시작 위치로
                           SetAIPath();                                   // 경로 다시설정
                           
                           m_snowBallMgr.SetLocalScale();                 // 스노우볼 크기 초기화
                           m_snowBallMeshRdr.enabled = true;              // 스노우볼 렌더링 켜기
                           m_characterMeshRdr.enabled = true;             // 캐릭터 렌더러 끄기
                       })
                       .AppendInterval(3f)
                       .OnComplete(() =>
                       {
                           SetAnim_Push();                                  // 애니메이션 Push 변경
                           m_isMoving = true;                               // 움직임 true
                           m_snowBallMgr.SetSphereCollider(true);           // SnowBall 콜라이더 On
                           m_snowBallMgr.SetSnowBallSize(true, false);      // SnowBall 사이즈 Up
                           StartCoroutine(PlayerMoving());                  // 움직임 시작!
                       });
    }


    public override void SetWhenInTheSnowBall()
    {
        m_playerState = ePlayerState.Death;

        m_snowBallMeshRdr.enabled = false;                                 // 스노우볼 렌더링 끄기
        m_characterMeshRdr.enabled = false;                                // 캐릭터 렌더러 끄기


        m_splashEffect.SetActive(false);                                   // 플레이어 달리는 이펙트 끄기


        StopPlayerMoving();                                               // 플레이어 움직임 Stop


        playerGameOverSeq = DOTween.Sequence()
                       .AppendInterval(3.2f)
                       .AppendCallback(() =>
                       {
                           SetPlayerToStartPos();                         // 플레이어 초기 시작 위치로
                           SetAIPath();                                   // 경로 다시설정


                           m_snowBallMgr.SetLocalScale();                 // 스노우볼 크기 초기화
                           m_snowBallMeshRdr.enabled = true;              // 스노우볼 렌더링 켜기

                           m_characterMeshRdr.enabled = true;             // 캐릭터 렌더링 켜기
                       })
                       .AppendInterval(3f)
                       .OnComplete(() =>
                       {
                           SetAnim_Push();
                           m_isMoving = true;
                           //m_snowBallMgr.RotateSnowBall();
                           m_snowBallMgr.SetSphereCollider(true);
                           m_snowBallMgr.SetSnowBallSize(true, false);
                           StartCoroutine(PlayerMoving());
                       });
    }
    #endregion


    #region 플레이어 이동관련
    private int m_pathCount = 0;
    public void SetAIPath()
    {
        m_aiPathTf.Clear();
        m_pathCount = 0;

        // === Ai Path 설정 === //
        Transform[] a_aiPathTf;

        a_aiPathTf = InGameManager.m_aiPathMgr.SetPathRoot(m_level);

        for (int n = 0; n < a_aiPathTf.Length; n++)
            m_aiPathTf.Add(a_aiPathTf[n]);
        // === Ai Path 설정 === //
    }

    /// <summary>
    /// (GameState = Play) Ai 경로따라 움직이는 코루틴함수               고치는중~!~
    /// </summary>
    public override IEnumerator PlayerMoving()
    {
        if (m_aiPathTf.Count <= m_pathCount)
        {
            m_isMoving = false;
            StopCoroutine(PlayerMoving());
        }

        if (m_isMoving == true)
        {
            // === 지경 Path로 이동 === //
            float a_dis = (this.transform.position - m_aiPathTf[m_pathCount].position).sqrMagnitude;
            if (a_dis < 150f)
                m_pathCount += 1;
            else
            {
                this.transform.Translate(Vector3.forward * m_playerMovSpd, Space.Self);
                this.transform.DOLookAt(m_aiPathTf[m_pathCount].position, 1.5f);
            }
            // === 지경 Path로 이동 === //

            yield return new WaitForSeconds(0.01f);

            StartCoroutine(PlayerMoving());
        }
    }

    /// <summary>
    /// (GameState = End) Ai 게임 Clear/GameOver시 호출되는 함수
    /// </summary>
    public override void StopPlayerMoving()
    {
        base.StopPlayerMoving();

        m_snowBallMeshRdr.enabled = false;                                  // 눈덩이 메쉬렌더러 끄기
    }

    /// <summary>
    /// (GameState = Play)눈덩이 커질때마다 호출되는 함수 (증가)
    /// </summary>
    public override void SetAnimSpeedUp()
    {
        float a_rnd = Random.Range(.8f, 1.1f);
        m_animCtrl.speed += a_rnd;
        m_playerMovSpd += a_rnd;
    }
    /// <summary>
    /// (GameState = Clear/End)골인/플레이어죽음 때 호출되는 함수
    /// </summary>
    public override void SetAnimSpeedOrigin()
    {
        base.SetAnimSpeedOrigin();
    }
    #endregion


    #region 플레이어 충돌 이펙트
    public override void PlayCrashEffect(int a_num)
    {
        base.PlayCrashEffect(a_num);
    }
    #endregion


    protected override void OnTriggerEnter(Collider other)
    {
        if (m_playerState == ePlayerState.Run)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("FastZone"))
            {
                playerFastSeq = DOTween.Sequence()
                                       .AppendCallback(() =>
                                       {
                                           m_playerRunSplashEffect.SetFloat("Speed", 70);
                                           m_playerMovSpd += 1;
                                       })
                                       .AppendInterval(2f)
                                       .OnComplete(() =>
                                       {
                                           m_playerRunSplashEffect.SetFloat("Speed", 18);
                                           m_playerMovSpd -= 1;
                                       });
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("GoalLine"))
            {
                StopPlayerMoving();
                SetAnim_Clear();
            }
        }
    }


}