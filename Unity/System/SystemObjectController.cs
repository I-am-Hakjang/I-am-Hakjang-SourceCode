using System;
using System.Collections;
using UnityEngine;

namespace Hakjang
{
	[DefaultExecutionOrder((int)UpdateOrderGroup.ROOT)]
	public class SystemObjectController : MonoBehaviour
	{
        #region Property
        #endregion

        #region Field
        #endregion

        #region Method
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        #endregion
    }
}
