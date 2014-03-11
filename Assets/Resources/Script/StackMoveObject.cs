using UnityEngine;
using System.Collections.Generic;

public class StackMoveObject : MonoBehaviour {

	[SerializeField]
	List<AutoMoveObject> listMoveObject = new List<AutoMoveObject>();

	public List<AutoMoveObject> ListMoveObject{
		get{
			return listMoveObject;
		}
	}
	// Update is called once per frame
	void Update () {
		if(listMoveObject.Count > 0 && listMoveObject[0].IsDone){
			listMoveObject.Remove(listMoveObject[0]);
			listMoveObject[0].Pause = false;
		}
	}
	public AutoMoveObject CurrentMoveObject(){
		if(listMoveObject.Count > 0)
			return listMoveObject[0];
		else
			return null;
	}

}
