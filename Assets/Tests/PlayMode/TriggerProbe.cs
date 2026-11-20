using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using TargetRange.Core;
using TargetRange.Weapon;

namespace TargetRange.Tests.PlayMode
{
    /// <summary>Temporary experiment: why does the left mouse click not reach the pistol?</summary>
    public class TriggerProbe
    {
        [UnityTest, Timeout(300000)]
        public IEnumerator Probe()
        {
            yield return SceneManager.LoadSceneAsync("Range");
            for (int i = 0; i < 120; i++) yield return null;

            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Space));
            for (int i = 0; i < 20; i++) yield return null;

            var hand = Object.FindObjectsByType<NearFarInteractor>().First(h => h.GetComponentsInParent<Transform>(true).Any(t => t.name.Contains("Right")));
            var pistol = Object.FindAnyObjectByType<Pistol>();
            var grab = pistol.GetComponent<XRGrabInteractable>();
            grab.interactionManager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)hand, grab);
            for (int i = 0; i < 20; i++) yield return null;

            var device = InputSystem.devices.OfType<XRSimulatedController>().First(d => d.usages.Contains(UnityEngine.InputSystem.CommonUsages.RightHand));
            Debug.Log($"TP held={pistol.IsHeld} state={Object.FindAnyObjectByType<GameManager>().State}");

            for (int i = 0; i < 30; i++)
            {
                InputSystem.QueueStateEvent(Mouse.current, new MouseState().WithButton(MouseButton.Left));
                yield return null;
                if (i % 5 == 0)
                    Debug.Log($"TP frame {i}: mouseLeft={Mouse.current.leftButton.isPressed} deviceTrigger={device.trigger.ReadValue():0.00} " +
                              $"activateValue={hand.activateInput.ReadValue():0.00} performed={hand.activateInput.ReadIsPerformed()} shouldActivate={hand.shouldActivate} " +
                              $"shots={Object.FindAnyObjectByType<GameManager>().Score.Shots}");
            }
            InputSystem.QueueStateEvent(Mouse.current, new MouseState());
            Assert.Pass();
        }
    }
}
