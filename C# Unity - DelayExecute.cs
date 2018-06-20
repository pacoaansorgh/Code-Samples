using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

// DelayExecute can be accessed globally from a static instance, due to inheriting from MonoBehaviourSingleton (an extension to monobehaviour that implements it as a singleton). Use it to execute actions after a delay outside monobehaviour.
public class DelayExecute : MonoBehaviourSingleton<DelayExecute> {

    // Dictionary of coroutines (actions) with key values so we can stop them later if needed.
    private Dictionary<string, Coroutine> stoppableRoutines = new Dictionary<string, Coroutine>();

    // Execute given action after given delay.
    public void DelayedExecute(float delayInSeconds, Action toExecute) {
        StartCoroutine(internalExecute(delayInSeconds, toExecute));
    }

    // Execute given action after given delay, and store it in a dictionary with a key value so we can stop it later.
    public void DelayedExecute(float delayInSeconds, Action toExecute, string key) {
        Coroutine newRoutine = StartCoroutine(internalExecute(delayInSeconds, toExecute));
        stoppableRoutines.Add(key, newRoutine);
    }

    // Find and stop the coroutine with the given key.
    public void StopDelayedExecute(string key) {
        if (stoppableRoutines.ContainsKey(key)) {
            StopCoroutine(stoppableRoutines[key]);
            stoppableRoutines.Remove(key);
        }
    }

    // Start a coroutine with the given action and delay.
    IEnumerator internalExecute(float delay, Action action) {
        yield return new WaitForSeconds(delay);
        action();
    }
}
