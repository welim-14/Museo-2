using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TelepuertoEscena : MonoBehaviour
{
	public string sceneName;

	void OnTriggerEnter(Collider Other)
	{
		if(Other.tag == "Player"){
			SceneManager.LoadScene (sceneName);
		}
	}

}
