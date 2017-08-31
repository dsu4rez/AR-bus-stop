/*==============================================================================
Copyright (c) 2010-2014 Qualcomm Connected Experiences, Inc.
All Rights Reserved.
Confidential and Proprietary - Protected under copyright and other laws.
==============================================================================*/

using UnityEngine;
using System.Collections;

namespace Vuforia
{
	/// <summary>
	/// A custom handler that implements the ITrackableEventHandler interface.
	/// </summary>
	public class MyTrackableEventHandler : MonoBehaviour,
	ITrackableEventHandler
	{	

		#region PRIVATE_MEMBER_VARIABLES

		private TrackableBehaviour mTrackableBehaviour;


		#endregion // PRIVATE_MEMBER_VARIABLES



		#region UNTIY_MONOBEHAVIOUR_METHODS

		void Start()
		{
			mTrackableBehaviour = GetComponent<TrackableBehaviour>();
			if (mTrackableBehaviour)
			{
				mTrackableBehaviour.RegisterTrackableEventHandler(this);
			}
		}

		#endregion // UNTIY_MONOBEHAVIOUR_METHODS



		#region PUBLIC_METHODS

		/// <summary>
		/// Implementation of the ITrackableEventHandler function called when the
		/// tracking state changes.
		/// </summary>
		public void OnTrackableStateChanged(
			TrackableBehaviour.Status previousStatus,
			TrackableBehaviour.Status newStatus)
		{
			if (newStatus == TrackableBehaviour.Status.DETECTED ||
				newStatus == TrackableBehaviour.Status.TRACKED ||
				newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
			{
				OnTrackingFound();
			}
			else
			{
				OnTrackingLost();
			}
		}

		#endregion // PUBLIC_METHODS



		#region PRIVATE_METHODS

		private GameObject appController;//app controller game object
		public Transform appControllerPrefab;//app controller orefab

		private void OnTrackingFound()
		{
			Renderer[] rendererComponents = GetComponentsInChildren<Renderer>(true);
			Collider[] colliderComponents = GetComponentsInChildren<Collider>(true);

			// Enable rendering:
			foreach (Renderer component in rendererComponents)
			{
				component.enabled = true;
			}

			// Enable colliders:
			foreach (Collider component in colliderComponents)
			{
				component.enabled = true;
			}

			if (mTrackableBehaviour.TrackableName == "stop") { //if the image recognized is "stop", instantiate the app controller prefab

				appController = Instantiate(appControllerPrefab, new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

			}

			Debug.Log("Trackable " + mTrackableBehaviour.TrackableName + " found");
		}
			

		private void OnTrackingLost()
		{
			Renderer[] rendererComponents = GetComponentsInChildren<Renderer>(true);
			Collider[] colliderComponents = GetComponentsInChildren<Collider>(true);

			// Disable rendering:
			foreach (Renderer component in rendererComponents)
			{
				component.enabled = false;
			}

			// Disable colliders:
			foreach (Collider component in colliderComponents)
			{
				component.enabled = false;
			}

			Debug.Log("Trackable " + mTrackableBehaviour.TrackableName + " lost");

			//if the image recognized lost
			Destroy (appController);//destroy the app controller

			GameObject goToDestroy = GameObject.Find("ArrivalsPrefabScroll(Clone)") as GameObject;
			if(goToDestroy) Destroy (goToDestroy);//destroy the arrivals panel

			goToDestroy = GameObject.Find("ArrivalsPrefabLoading(Clone)") as GameObject;
			if(goToDestroy) Destroy (goToDestroy);//destroy the loading

			goToDestroy = GameObject.Find("AppController(Clone)") as GameObject;
			if(goToDestroy) Destroy (goToDestroy);//destroy the app controller

		}

		#endregion // PRIVATE_METHODS
	}
}

