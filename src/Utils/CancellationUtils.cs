// ----------------------------------------------------------------------------
// The Proprietary or MIT License
// Copyright (c) 2022-2022 RFS_6ro <rfs6ro@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Threading;

namespace R6Tasks.Utils
{
    public static class CancellationUtils
    {
        public static CancellationToken RefreshToken(ref CancellationTokenSource tokenSource)
        {
            Cancel(tokenSource);
            tokenSource = new CancellationTokenSource();
            return tokenSource.Token;
        }
		
        public static CancellationToken RefreshToken()
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            return tokenSource.Token;
        }

        public static CancellationToken RefreshTokenWithTimeout(ref CancellationTokenSource tokenSource, int milliseconds)
        {
            if (milliseconds <= 0)
            {
                return RefreshToken(ref tokenSource);
            }
			
            Cancel(tokenSource);
            tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(milliseconds);
            return tokenSource.Token;
        }

        public static CancellationToken RefreshTokenWithTimeout(int milliseconds)
        {
            if (milliseconds <= 0)
            {
                return RefreshToken();
            }
			
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(milliseconds);
            return tokenSource.Token;
        }

        public static CancellationToken RefreshTokenWithOnDestroyToken(ref CancellationTokenSource tokenSource, UnityEngine.GameObject gameObject, out CancellationTokenSource linkedTokenSource)
        {
            Cancel(tokenSource);
            tokenSource = new CancellationTokenSource();

            CancellationToken ctsDestroy = gameObject.GetCancellationTokenOnDestroy();
            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ctsDestroy, tokenSource.Token);
			
            return linkedTokenSource.Token;
        }

        public static void Cancel(CancellationTokenSource tokenSource)
        {
            try
            {
                tokenSource?.Cancel();
                tokenSource?.Dispose();
            }
            catch (ObjectDisposedException) { }
        }

        public static void Cancel(ref CancellationTokenSource tokenSource)
        {
            try
            {
                tokenSource?.Cancel();
                tokenSource?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                tokenSource = new CancellationTokenSource();
            }
        }
    }
}
