using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

/**
 * Developed by David Suarez Esteban
 * email: davidsuarez93@hotmail.com
 **/

namespace Vuforia
{
	public class ArrivalsPanelController: MonoBehaviour
	{

		void Start(){//when the parent prefab is instantiated it starts
			
			StartCoroutine (GetStopInfoCoroutine());//launch coroutine with all the functionalities
		}
			
		/**
		 * 
		 * Coroutine that launch all the functionalities asincying
		 * 
		 **/	
		private string imagePath="";//path to store the screenshot
		private string imageText="";
		private string stopID="";
		private string stopArrivals="";

		IEnumerator GetStopInfoCoroutine(){

			this.imagePath = setScreenshotPath ();//set screenshot store path

			Application.CaptureScreenshot (imagePath,1); //capture screenshot
			yield return new WaitForSeconds(1);//wait 1 second to store the image succesfully

			this.instantiateLoadingPanel ();//instance the AR panel over the bus stop signal

			string encodedImage = this.encodeImageBase64 (imagePath);//encode the screenshot in Base64
			yield return StartCoroutine(getTextFromImage(encodedImage));//wait until get the response with the extracted text from the screenshot

			stopID = this.getBusStopID (imageText);//extract the bus stop ID from the text extracted

			yield return StartCoroutine(getStopInfo(stopID)); //wait until get response with arrivals info for the bus stop id

			ArrayList arrivalsList = this.parseInfoToList (stopArrivals);//parse xml response to list

			this.instantiateArrivalsPanel (arrivalsList);//show arrivals list in AR panel
		}

		/**
		 * 
		 * Function that set the screenshot store path depending of the paltform where it runs
		 * 
		 **/
		private string setScreenshotPath(){

			string _imagePath = "";
			#if UNITY_IOS || UNITY_ANDROID
			_imagePath="/Screenshot.png";
			#endif

			#if UNITY_EDITOR
			_imagePath = "tmp/Screenshot.png";
			#endif

			return _imagePath;
		}

		/**
		 * 
		 * Function to encode a PNG image in Base64 and return the result in a String
		 * 
		 **/
		private string encodeImageBase64(String _imagePath){

			Texture2D texture = LoadPNG (_imagePath);//load PNG image from path

			string base64Img = "";
			base64Img = System.Convert.ToBase64String (texture.EncodeToJPG(20));

			return base64Img;
		}

		/**
		 * 
		 * Function to load PNG image from path into a Texture2D object which is returned
		 * 
		 **/
		private static Texture2D LoadPNG(string _imagePath) {

			Texture2D texture = null;
			byte[] fileData;

			#if UNITY_IOS || UNITY_ANDROID
			if (File.Exists(Application.persistentDataPath+_imagePath))     {
				fileData = File.ReadAllBytes(Application.persistentDataPath+_imagePath);
				texture = new Texture2D(1, 1);
				texture.LoadImage(fileData); //..this will auto-resize the texture dimensions.
			}
			#endif

			#if UNITY_EDITOR
			if (File.Exists(_imagePath)){
				fileData = File.ReadAllBytes(_imagePath);
				texture = new Texture2D(1, 1);
				texture.LoadImage(fileData); //..this will auto-resize the texture dimensions.
				Debug.Log ("Imagen cargada en textura");
			}
			#endif

			return texture;
		}
			

		/**
		 * 
		 * Function that make a post call to the OCR web service using the base64 encoded image
		 * 
		 **/
		private string ocrPostURL = "http://api.ocr.space/parse/image";//URL of OCR API post call

		IEnumerator getTextFromImage(string _encodedImage) {

			WWWForm form = new WWWForm();//call form
			form.AddField( "apikey", "<yourapikeyhere>" );
			form.AddField( "language", "spa" );
			form.AddField( "isOverlayRequired", "true" );
			form.AddField( "base64Image", "data:image/png;base64,"+ _encodedImage );

			WWW www = new WWW(ocrPostURL, form);
			yield return StartCoroutine(WaitForRequest(www));//launch async coroutine and wait until it ends

			imageText = www.text;//save response in global variable

		}

		/**
		 * 
		 * IEnumerator for wait call response
		 * 
		 **/
		IEnumerator WaitForRequest(WWW _www)
		{
			yield return _www;
			if (_www.error == null)
			{
				Debug.Log("WWW Ok!: " + _www.text);
			}
			else
			{
				Debug.Log("WWW Error: " + _www.error);
			}
		}

		/**
		 * 
		 * Function that returns a string with the bus stop ID from the OCR response (I didn't find any good free JSON parser >.< )
		 * 
		 **/
		private string getBusStopID(string _imageText){

			string myStopID = "";

			byte[] bytes = Encoding.Default.GetBytes(_imageText);
			string imageTextUTF8 = Encoding.UTF8.GetString(bytes);

			int pos_ini = 0;
			int pos_end = 0;

			ArrayList wordList = new ArrayList();

			while(imageTextUTF8.IndexOf("WordText\":\"", pos_end)>-1){ //store extracted words in a list

				pos_ini = imageTextUTF8.IndexOf("WordText\":\"", pos_end)+"WordText\":\"".Length;
				pos_end = imageTextUTF8.IndexOf("\",", pos_ini);

				wordList.Add(imageTextUTF8.Substring(pos_ini,pos_end-pos_ini));

			}

			foreach (string word in wordList) {//look every word

				int n;
				if (int.TryParse (word, out n)) //if this word is a number
					myStopID = word; //this is considered as the bus stop ID

			}

			return myStopID; 
		}

		/**
		 * 
		 * Function that make a post call to the EMT (bus company of Madrid) web service using the bus stop ID
		 * 
		 **/
		private string arrivalsPostURL = "https://openbus.emtmadrid.es:9443/emt-proxy-server/last/geo/GetArriveStop.php";

		IEnumerator getStopInfo(string _stopID){

			WWWForm form = new WWWForm();//call form
			form.AddField( "idClient", "<youridclienthere>" );
			form.AddField( "passKey", "<yourpasskeyhere>" );
			form.AddField( "idStop", _stopID );

			WWW www = new WWW (arrivalsPostURL,form);

			yield return StartCoroutine(WaitForRequest(www));//launch async coroutine and wait until it ends

			stopArrivals = www.text;//store response in a global variable

		}

		/**
		 * 
		 * Function that parse arrivals response into a list of arrivals (I didn't find any good free JSON parser >.< )
		 * 
		 **/
		private ArrayList parseInfoToList(string _stopArrivals){

			byte[] bytes = Encoding.Default.GetBytes(_stopArrivals);
			string stopArrivalsUTF8 = Encoding.UTF8.GetString(bytes);

			int pos_ini = 0;
			int pos_end = 0;

			ArrayList arrivalsList = new ArrayList(); //arrivals list

			string _stopId = "";
			string _lineId = "";
			string _destination = "";
			string _busTimeLeft = "";
			string _busDistance = "";

			while(stopArrivalsUTF8.IndexOf("stopId\":", pos_end)>-1){

				pos_ini = stopArrivalsUTF8.IndexOf("stopId\":", pos_end)+"stopId\":".Length;
				pos_end = stopArrivalsUTF8.IndexOf(",", pos_ini);
				_stopId = stopArrivalsUTF8.Substring (pos_ini, pos_end - pos_ini);


				pos_ini = stopArrivalsUTF8.IndexOf("lineId\":\"", pos_end)+"lineId\":\"".Length;
				pos_end = stopArrivalsUTF8.IndexOf("\",", pos_ini);
				_lineId = stopArrivalsUTF8.Substring (pos_ini, pos_end - pos_ini);

				pos_ini = stopArrivalsUTF8.IndexOf("destination\":\"", pos_end)+"destination\":\"".Length;
				pos_end = stopArrivalsUTF8.IndexOf("\",", pos_ini);
				_destination = stopArrivalsUTF8.Substring (pos_ini, pos_end - pos_ini);

				pos_ini = stopArrivalsUTF8.IndexOf("busTimeLeft\":", pos_end)+"busTimeLeft\":".Length;
				pos_end = stopArrivalsUTF8.IndexOf(",", pos_ini);
				_busTimeLeft = stopArrivalsUTF8.Substring (pos_ini, pos_end - pos_ini);

				pos_ini = stopArrivalsUTF8.IndexOf("busDistance\":", pos_end)+"busDistance\":".Length;
				pos_end = stopArrivalsUTF8.IndexOf(",", pos_ini);
				_busDistance = stopArrivalsUTF8.Substring (pos_ini, pos_end - pos_ini);

				string[] arrive = new string[] { _stopId, _lineId, _destination, _busTimeLeft, _busDistance };
				arrivalsList.Add(arrive);

			}

			return arrivalsList;
		}


		/**
		 * 
		 * Function that instantiates the AR panel while the info is loaded
		 * 
		 **/
		private GameObject arrivalsPanelGO;
		private GameObject imageTargetGO;

		private GameObject arrivalsLoadingGO;
		public Transform arrivalsLoadingPrefab;

		private GameObject arrivalsScrollGO;
		public Transform arrivalsScrollPrefab;

		private void instantiateLoadingPanel(){

			imageTargetGO = GameObject.FindWithTag("ImageTarget") as GameObject;

			Instantiate(arrivalsLoadingPrefab);//instantiate the loading AR panel as child of the ImageTarget game object
			arrivalsLoadingGO = GameObject.Find("ArrivalsPrefabLoading(Clone)") as GameObject;
			arrivalsLoadingGO.transform.SetParent (imageTargetGO.transform);
		}

		/**
		 * 
		 * Function that instantiates the AR panel of arrivals and show a list of arrivals on it
		 * 
		 **/

		public GameObject scrollElementPrefab;

		private void instantiateArrivalsPanel (ArrayList _arrivalsList){

			Destroy (arrivalsLoadingGO);//destroy the previous arrivals loading AR panel

			Instantiate(arrivalsScrollPrefab);//instantiate the arrivals AR panel as child of the ImageTarget game object
			arrivalsScrollGO = GameObject.Find("ArrivalsPrefabScroll(Clone)") as GameObject;
			arrivalsScrollGO.transform.SetParent (imageTargetGO.transform);

			Transform textGameObjectAux;
			Text textAux;

			GameObject stopInfoGO = GameObject.Find ("StopInfo") as GameObject;//find game object to set the text with the bus stop ID
			Text textStopInfo = stopInfoGO.transform.GetChild (0).GetComponent<Text> ();
			textStopInfo.text = "Nº de parada: "+this.stopID;

			GameObject scrollContent = GameObject.Find ("ScrollContent") as GameObject;//find the scroll layout game 
			RectTransform rt_scrollContent = scrollContent.GetComponent<RectTransform> ();

			for (int i = 0; i < _arrivalsList.Count; i++) {//instantiate an element in the scroll layout for each arrival in the list

				GameObject newElement = (GameObject)Instantiate(scrollElementPrefab); 
				newElement.transform.SetParent(rt_scrollContent,false);//(false is important to mantain the prefab size and position)

				string[] arrivalsArray = (string[])_arrivalsList [i];

				textGameObjectAux = newElement.transform.GetChild (0).GetChild (0);//text child that represents the bus line
				textAux = textGameObjectAux.GetComponent<Text> ();
				textAux.text = arrivalsArray [1];//set text with bus line

				textGameObjectAux = newElement.transform.GetChild (1).GetChild (0);//text child that represents the arrival time
				textAux = textGameObjectAux.GetComponent<Text> ();
				textAux.text = secondsToMinutes (arrivalsArray [3]);//set text with the arrival time

			}
			
		}

		/**
		 * 
		 * Function that parse a string of seconds to a string of minutes and returns the value
		 * 
		 **/
		private String secondsToMinutes (string _seconds){

			string minutes = "";

			int secondsInt = Int32.Parse (_seconds);

			if (secondsInt == 999999) {
				minutes = ">20 minutos";
			} else if (secondsInt == 0) {
				minutes = "Llegando";
			}
			else{
				minutes = "" + secondsInt / 60 + " minutos";
			}

			return minutes;
		}


	}
}

