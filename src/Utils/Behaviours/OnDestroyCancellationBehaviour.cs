// ----------------------------------------------------------------------------
// The Proprietary or MIT License
// Copyright (c) 2022-2022 RFS_6ro <rfs6ro@gmail.com>
// ----------------------------------------------------------------------------

using System.Threading;
using UnityEngine;

namespace R6Tasks.Utils.Behaviours
{
    public class OnDestroyCancellationBehaviour : MonoBehaviour
    {
        private CancellationTokenSource _cts;

        public CancellationToken Token
        {
            get
            {
                if (_cts == null)
                {
                    _cts = new CancellationTokenSource();
                }

                return _cts.Token;
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}