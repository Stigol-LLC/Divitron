using UnityEngine;
using System;

public class PlayerChild : MonoBehaviour {
	private bool isCollision = false;
	void OnTriggerEnter2D(Collider2D other) {
		if(!isCollision && other.GetComponent<PlayerChild>() == null){
			isCollision = true;
			RunDestroyEffects();
		}
	}
	public bool IsCollision{
		get{
			return isCollision;
		}
	}
	public void RunDestroyEffects(){
		//GameObject go = (GameObject)Instantiate(Resources.Load("ParticlePlayerDestroy"));
		//go.name = "ParticlePlayerDestroy";
		//go.transform.parent = transform.parent;
		//go.transform.position = transform.position;

		//transform.localScale = Vector2.zero;
		collider2D.enabled = false;
		//go.GetComponent<ParticleSystem>().Play();
	}
}
