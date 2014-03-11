using UnityEngine;
using System.Collections.Generic;

public class StackMoveObject : MonoBehaviour {

	[SerializeField]
	List<AutoMoveObject> listMoveObject = new List<AutoMoveObject>();
	int currentIndex = 0;

	public List<AutoMoveObject> ListMoveObject{
		get{
			return listMoveObject;
		}
	}
	public void Reset(){
		currentIndex = 0;
		foreach(var o in listMoveObject){
			o.Reset();
		}
	}
	// Update is called once per frame
	void Update () {
		if(listMoveObject.Count > currentIndex && listMoveObject[currentIndex].IsDone){
			currentIndex++;
			listMoveObject[currentIndex].Pause = false;
		}
	}
	public AutoMoveObject CurrentMoveObject(){
		if(listMoveObject.Count > currentIndex)
			return listMoveObject[currentIndex];
		else
			return null;
	}

}
