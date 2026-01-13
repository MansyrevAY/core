using System;
using System.Threading.Tasks;
using Core.Infra.Log;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class TaskUtils
{
    public static async void Forget(this UniTask task)
    {
        try
        {
            await task;
        }
        catch (TaskCanceledException e)
        {
            Log.Warning($"Task cancelled: {e.Message}");
        }
        catch (Exception e)
        {
            throw e;
        }
    }
}
