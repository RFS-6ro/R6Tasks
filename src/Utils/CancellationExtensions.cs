// ----------------------------------------------------------------------------
// The Proprietary or MIT License
// Copyright (c) 2022-2022 RFS_6ro <rfs6ro@gmail.com>
// ----------------------------------------------------------------------------

using System.Threading;
using R6Tasks.Utils.Behaviours;
using UnityEngine;

namespace R6Tasks.Utils
{
    public static class CancellationExtensions
    {
        public static CancellationToken GetCancellationTokenOnDestroy(this GameObject gameObject)
        {
            OnDestroyCancellationBehaviour behaviour = gameObject.GetComponent<OnDestroyCancellationBehaviour>();
            if (behaviour == null)
            {
                behaviour = gameObject.AddComponent<OnDestroyCancellationBehaviour>();
            }

            return behaviour.Token;
        }
    }
}