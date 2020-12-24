﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraManager : MonoBehaviour
{
    [Header("(GameState = Start) 관련")]
    [SerializeField] Transform m_camStartPosTf = null;            // (GameState = Start)게임 시작시 플레이어를 뒤에서 바라볼 위치 
    [SerializeField] Transform m_playerTf = null;                 // (GameState = Start)플레이어를 부모위치로 잡을 Tf
    [SerializeField] Transform m_lookPosTf = null;                // (GameState = Start)카메라가 이동중 바라볼 Tf
    [SerializeField] Transform m_originParentTf = null;


    public Quaternion TargetRotation;                              // (GameState = Play)최종적으로 축적된 Gap이 이 변수에 저장됨.
    public float m_rotationSpeed = 8;                             // (GameState = Play)터치시 플레이어 회전 스피드.
    [SerializeField] private float m_originRotationSpeed = 5;                             // (GameState = Play)터치시 플레이어 회전 스피드.
    private Vector3 Gap = Vector3.zero;                            // (GameState = Play)회전 축적 값.
    private Vector3 m_followOffset = new Vector3(0, 60, -90);      // (GameState = Play)카메라 Zoom In/Out 관련 벡터



    private void Start()
    {
        m_originRotationSpeed = m_rotationSpeed;
    }
    

    /// <summary>
    /// (GameState = Play) 화면 터치시 화면회전하는 부부
    /// </summary>
    private void LateUpdate()
    {
        if(InGameManager.uniqueInstance.m_curGameState == InGameManager.eGameState.Play)
        {
            #region (GameState = Play) 카메라 회전 부분

            if(Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if(InGameManager.m_plyMgr.m_playerState == ePlayerState.Run)
                {
                    if (Input.touchCount > 0)
                 {
                     Touch a_touch = Input.GetTouch(0);
            
                     if (a_touch.phase == TouchPhase.Began)
                     {
            
                     }
                     if (a_touch.phase == TouchPhase.Moved)
                     {
                         if (transform.rotation != TargetRotation)
                             m_playerTf.transform.rotation = Quaternion.Slerp(m_playerTf.transform.rotation, TargetRotation, m_rotationSpeed * Time.deltaTime);
            
                         //Gap.x += Input.GetAxis("Mouse Y") * m_rotationSpeed * -1;
                         Gap.y += Input.GetAxis("Mouse X") * m_rotationSpeed;
            
                         Gap.y += InGameManager.m_joystick.Horizontal;
                         Gap.y = Mathf.Clamp(Gap.y, -80f, 80f);
            
                         TargetRotation = Quaternion.Euler(Gap);
                     }
                 }
                }
            }
            else
            {
                if (InGameManager.m_plyMgr.m_playerState == ePlayerState.Run)
                {
                    if (Input.GetMouseButton(0))
                    {
                        if (transform.rotation != TargetRotation)
                            m_playerTf.transform.rotation = Quaternion.Slerp(m_playerTf.transform.rotation, TargetRotation, m_rotationSpeed * Time.deltaTime);

                        //Gap.x += Input.GetAxis("Mouse Y") * m_rotationSpeed * -1;
                        Gap.y += Input.GetAxis("Mouse X") * m_rotationSpeed;

                        Gap.y += InGameManager.m_joystick.Horizontal;
                        Gap.y = Mathf.Clamp(Gap.y, -80f, 80f);

                        TargetRotation = Quaternion.Euler(Gap);
                    }
                }
            }
            #endregion
        }
    }


    /// <summary>
    /// (GameState = Ready)카메라 => 플레이어 뒤로 이동 함수 (연출)
    /// </summary>
    public void CamToPlayerProduction(bool a_gameStart = false)
    {
        //Sequence camPdSeq;

        m_camStartPosTf = GameObject.FindGameObjectWithTag("CameraStartPos").transform;

        m_rotationSpeed = m_originRotationSpeed;

        this.transform.DOMove(m_camStartPosTf.position, 1f);
        this.transform.DOLocalRotate(m_camStartPosTf.eulerAngles, 1f)
                          .OnComplete(() =>
                          {
                              this.transform.SetParent(m_playerTf.transform);
                              this.transform.DOLookAt(m_lookPosTf.position, .5f).SetDelay(1.5f)
                                                .OnComplete(() =>
                                                {
                                                    if(a_gameStart == false)
                                                        InGameManager.uniqueInstance.PlayPlayerGame();
                                                });
                          });

        //camPdSeq = DOTween.Sequence()
        //                  .OnStart(() =>
        //                  {
        //                      m_rotationSpeed = m_originRotationSpeed;
        //                  })
        //                  .Append(this.transform.DOMove(m_camStartPosTf.position, 1f))
        //                  .Join(this.transform.DOLocalRotate(m_camStartPosTf.eulerAngles, 1f))
        //                  .Append(this.transform.DOLookAt(m_lookPosTf.position, .5f))
        //                  .AppendCallback(() =>
        //                  {
        //                      this.transform.SetParent(m_playerTf.transform);
        //                  })
        //                  .AppendInterval(1.5f)
        //                  .OnComplete(() =>
        //                  {
        //                      InGameManager.uniqueInstance.PlayPlayerGame();
        //                  });
    }

    
    /// <summary>
    /// 카메라를 원래 부모 오브젝트로 이동
    /// </summary>
    public void SetCamParent()
    {
        this.transform.SetParent(m_originParentTf.transform);
    }


    /// <summary>
    /// (GameState = Play) 스노우볼 크기에 따라 호출되는 함수
    /// </summary>
    public void SetFollowOffset(bool a_zoomOut)
    {
        if(a_zoomOut == true)
        {
            // === 카메라 줌아웃 === //
            Vector3  a_followOffset = this.transform.localPosition + m_followOffset;
            // print("Cam Pos : " + a_followOffset);

            this.transform.DOLocalMove(a_followOffset, .6f)
                          .SetEase(Ease.InQuad);
            // === 카메라 줌아웃 === //


            // === 캐릭터 회전 감도 === //
            m_rotationSpeed -= 1.5f;
            // === 캐릭터 회전 감도 === //
        }
        else
        {
            // === 카메라 줌인 === //
            Vector3 a_followOffset = this.transform.localPosition - m_followOffset;
            //print("Cam Pos : " + a_followOffset);

            this.transform.DOLocalMove(a_followOffset, .6f)
                          .SetEase(Ease.InQuad);
            // === 카메라 줌인 === //


            // === 캐릭터 회전 감도 === //
            m_rotationSpeed += 1.5f;
            // === 캐릭터 회전 감도 === //
        }
    }
    
}