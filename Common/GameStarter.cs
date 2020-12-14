﻿using UnityEngine;
using System;
using Firebase.Extensions;
using Firebase.Analytics;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Purchasing;

namespace KahaGameCore.Common
{
    public class GameStarter 
    {
        public event Action OnStartToInitPlugins = null;
        public event Action OnCheckConnectionFailed = null;
        public event Action OnTimeOut = null;
        public event Action OnAllInited = null;

        private const float TIME_OUT_TIME = 15f;

        private void Start()
        {
            Debug.Log("GameStarter Start");

            if(OnStartToInitPlugins != null)
            {
                OnStartToInitPlugins();
            }

            GameUtility.CheckConnection(
                delegate (bool having)
                {
                    Debug.Log("CheckConnection: " + having);
                    if (having)
                    {
                        StartInitFirebase();
                    }
                    else
                    {
                        if(OnCheckConnectionFailed != null)
                        {
                            OnCheckConnectionFailed();
                        }
                    }
                }
            );
        }

        private void StartInitFirebase()
        {
            Debug.Log("StartInitFirebase");
            Task task = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
            task.ContinueWithOnMainThread(OnFirebaseInited);
            StartPreloadAd();
        }

        private void OnFirebaseInited(Task task)
        {
            Debug.Log("OnFirebaseInited");
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        }

        private void StartPreloadAd()
        {
            Debug.Log("StartPreloadAd");
            m_adTimer = TIME_OUT_TIME;
            AdvertisementManager.Instance.Init();
            Static.GeneralCoroutineRunner.Instance.StartCoroutine(IEWaitADLoading());
        }

        private float m_adTimer = 0f;
        private IEnumerator IEWaitADLoading()
        {
            Debug.Log("IEWaitADLoading:" + m_adTimer);

            if(m_adTimer <= 0f)
            {
                StartInitIAP();
                yield break;
            }

            if (AdvertisementManager.Instance.IsRewardVideoAdLoaded()
                && AdvertisementManager.Instance.IsInterstitialVideoAdLoaded())
            {
                Debug.Log("AD Loaded");
                StartInitIAP();
            }
            else
            {
                yield return null;
                m_adTimer -= Time.deltaTime;
                Static.GeneralCoroutineRunner.Instance.StartCoroutine(IEWaitADLoading());
            }
        }

        private void StartInitIAP()
        {
            Debug.Log("StartInitIAP");
            m_storeTimer = TIME_OUT_TIME;
            Static.GeneralCoroutineRunner.Instance.StartCoroutine(IEWaitStoreInited());
        }

        private float m_storeTimer = 0f;
        private IEnumerator IEWaitStoreInited()
        {
            Debug.Log("IEWaitStoreInited:" + m_storeTimer);
            if(m_storeTimer <= 0f)
            {
                if(OnTimeOut != null)
                {
                    OnTimeOut();
                }
                yield break;
            }

            if (CodelessIAPStoreListener.initializationComplete)
            {
                Debug.Log("Store Inited");
                if (OnAllInited != null)
                {
                    OnAllInited();
                }
            }
            else
            {
                yield return null;
                m_storeTimer -= Time.deltaTime;
                Static.GeneralCoroutineRunner.Instance.StartCoroutine(IEWaitStoreInited());
            }
        }
    }
}
