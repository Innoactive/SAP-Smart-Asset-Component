using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SAP.Creator.SmartAsset.Templates
{
    /// <summary>
    /// Each state is responsible for initializing and cleaning up itself.
    /// </summary>
    public struct StateTransition
    {
        public Action Init;
        public Action Exit;
    }

    /// <summary>
    /// Class for managing states ans state-transitions in Monobehaviours.
    /// </summary>
    public class StateMachine<T> : MonoBehaviour
    {
        protected Dictionary<T, StateTransition> stateTransitions = new Dictionary<T, StateTransition>();

        private T activeState;
        public T ActiveState
        {
            get { return activeState; }
            protected set
            {
                if (!EqualityComparer<T>.Default.Equals(activeState, value))
                {
                    if (stateTransitions.ContainsKey(activeState))
                    {
                        stateTransitions[activeState].Exit?.Invoke();
                    }

                    activeState = value;

                    if (stateTransitions.ContainsKey(activeState))
                    {
                        stateTransitions[activeState].Init?.Invoke();
                    }
                }
            }
        }
    }
}