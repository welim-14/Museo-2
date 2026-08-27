using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebURLScript : MonoBehaviour {

	// Use this for initialization
	void Start () 
	{
		
	}
	
	public void btnOne()
	{
		Application.OpenURL("http://www.google.com");

	}

	public void btnTwo()
	{
		Application.OpenURL("https://www.youtube.com/watch?v=8Qn_spdM5Zg");
		
	}

	public void btnTree()
	{
		Application.OpenURL("http://www.youtube.com");
		
	}
	// Update is called once per frame
	void Update () 
	{
		
	}
}
