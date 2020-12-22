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

    }


    /// <summary>
    /// (GameState = End)Ai GameOver 애니메이션 실행함수
    /// </summary>
    public void SetAiAnim_GameOver()
    {
        m_animCtrl.SetTrigger("GameOver");                                // 플레이어 Die 애니메이션 실행


        m_snowBallMeshRdr.enabled = false;                                // 눈덩이 메쉬렌더러 끄기


        m_splashEffect.SetActive(false);                                  // 플레이어 달리는 이펙트 끄기


        StopPlayerMoving();                                               // 플레이어 움직임 Stop


        test = DOTween.Sequence()
                       .AppendInterval(3.2f)
                       .AppendCallback(() =>
                       {
                           SetPlayerToStartPos();                         // 플레이어 초기 시작 위치로
                           SetAIPath();                                   // 경로 다시설정


                           m_snowBallMgr.SetLocalScale();                 // 스노우볼 크기 초기화
                           m_snowBallMeshRdr.enabled = true;              // 스노우볼 렌더링 켜기
                       })
                       .AppendInterval(1.5f)
                       .OnComplete(() =>
                       {
                           SetAnim_Push();
                           m_isMoving = true;
                           //m_snowBallMgr.RotateSnowBall();
                           m_snowBallMgr.SetSnowBallSize(true, false);
                           StartCoroutine(PlayerMoving());
                       });
    }


    public override void SetWhenInTheSnowBall()
    {
        m_snowBallMeshRdr.enabled = false;                                 // 스노우볼 렌더링 끄기
        m_characterMeshRdr.enabled = false;                                // 캐릭터 렌더러 끄기


        m_splashEffect.SetActive(false);                                  // 플레이어 달리는 이펙트 끄기


        StopPlayerMoving();                                               // 플레이어 움직임 Stop


        test = DOTween.Sequence()
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
        if (m_isMoving == true)
        {
            // === 지경 Path로 이동 === //
            float a_dis = (this.transform.position - m_aiPathTf[m_pathCount].position).sqrMagnitude;
            if (a_dis < 150f)
                m_pathCount += 1;
            else
            {
                this.transform.Translate(Vector3.forward * m_playerMovSpd, Space.Self);
                this.transform.DOLookAt(m_aiPathTf[m_pathCount].position, 2.5f);
            }
            // === 지경 Path로 이동 === //

            yield return null;

            StartCoroutine(PlayerMoving());
        }
    }

    /// <summary>
    /// (GameState = End) Ai 게임 Clear/GameOver시 호출되는 함수
    /// </summary>
    public override void StopPlayerMoving()
    {
        base.StopPlayerMoving();
    }

    /// <summary>
    /// (GameState = Play)눈덩이 커질때마다 호출되는 함수 (증가)
    /// </summary>
    public override void SetAnimSpeedUp()
    {
        float a_rnd = Random.Range(.7f, 1.3f);
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
        if(m_playerState == ePlayerState.Run)
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
            else if(other.gameObject.layer == LayerMask.NameToLayer("SnowBall"))
            {
                // === 스노우볼 크기 비교 === //
                if(other.gameObject.transform.localScale.x > m_snowBallObj.transform.localScale.x)
                {// Player 스노우볼이 더 크다면
                    print("Player get in the AI SnowBall");

                    test.Kill();
                    playerFastSeq.Kill();

                    SetWhenInTheSnowBall();
                    other.transform.parent.GetComponent<SnowBallManager>().CharacterInSnowBall();
                }
                else
                {// 내꺼(AI) 스노우볼이 더 크다면
                    print("AI get in the Player SnowBall");

                    m_snowBallMgr.ResetTweener();           // 트위너 초기화

                    //if(test.IsPlaying())
                        test.Kill();

                    //if (playerFastSeq.IsPlaying())
                        playerFastSeq.Kill();

                    SetWhenInTheSnowBall();
                    other.transform.parent.GetComponent<SnowBallManager>().CharacterInSnowBall();
                }
            }
        }
    }


}