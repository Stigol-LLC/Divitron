using UnityEngine;
using System;

public class PlayerChild : MonoBehaviour {
	private bool isCollision = false;
	void OnTriggerEnter2D(Collider2D other) {
		if(/*!isCollision && */other.GetComponent<PlayerChild>() == null){
			isCollision = true;
		}
	}
	public bool IsCollision{
		get{
			return isCollision;
		}
		set{
			isCollision = value;
		}
	}
}
