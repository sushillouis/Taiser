using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Core.Utilities {
	public class SingletonPun<T>: Singleton<T> where T : Singleton<T> {
		/// <summary>Cache field for the PhotonView on this GameObject.</summary>
        private PhotonView pvCache;

        /// <summary>A cached reference to a PhotonView on this GameObject.</summary>
        /// <remarks>
        /// If you intend to work with a PhotonView in a script, it's usually easier to write this.photonView.
        ///
        /// If you intend to remove the PhotonView component from the GameObject but keep this Photon.MonoBehaviour,
        /// avoid this reference or modify this code to use PhotonView.Get(obj) instead.
        /// </remarks>
        public PhotonView photonView
        {
            get
            {
                #if UNITY_EDITOR
                // In the editor we want to avoid caching this at design time, so changes in PV structure appear immediately.
                if (!Application.isPlaying || this.pvCache == null)
                {
                    this.pvCache = PhotonView.Get(this);
                }
                #else
                if (this.pvCache == null)
                {
                    this.pvCache = PhotonView.Get(this);
                }
                #endif
                return this.pvCache;
            }
        }
	}

	public class PersistentSingletonPun<T> : SingletonPun<T> where T : SingletonPun<T> {
		protected override void Awake() {
			base.Awake();
			DontDestroyOnLoad(gameObject);
		}
	}
}
