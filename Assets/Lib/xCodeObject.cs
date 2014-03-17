
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
/*
 * http://www.opensource.org/licenses/lgpl-2.1.php
 * xCodeObject class
 * for use with Unity
 * Copyright Matt Schoen 2010
 */

public class xCodeObject  {
	const int MAX_DEPTH = 1000;
	public enum Type { NULL, STRING, NUMBER, OBJECT, ARRAY, BOOL }
	public xCodeObject parent;
	public Type type = Type.NULL;
	public ArrayList list = new ArrayList();
	public ArrayList keys = new ArrayList();
	public string str;
	public double n;
	public bool b;
	
	public static xCodeObject nullJO { get { return new xCodeObject(xCodeObject.Type.NULL); } }
	public static xCodeObject obj { get { return new xCodeObject(xCodeObject.Type.OBJECT); } }
	public static xCodeObject arr { get { return new xCodeObject(xCodeObject.Type.ARRAY); } }
	
	public xCodeObject(xCodeObject.Type t) {
		type = t;
		switch(t) {
		case Type.ARRAY:
			list = new ArrayList();
			break;
		case Type.OBJECT:
			list = new ArrayList();
			keys = new ArrayList();
			break;
		}
	}
	public xCodeObject(bool b) {
		type = Type.BOOL;
		this.b = b;
	}
	public xCodeObject(float f) {
		type = Type.NUMBER;
		this.n = f;
	}
	public xCodeObject(Dictionary<string, string> dic) {
		type = Type.OBJECT;
		foreach(KeyValuePair<string, string> kvp in dic){
			keys.Add(kvp.Key);
			list.Add(kvp.Value);
		}
	}
	public xCodeObject() { }
	public xCodeObject(string str) {	//create a new xCodeObject from a string (this will also create any children, and parse the whole string)
		Debug.Log("xCodeObject" + str);

		if(str != null) {
			//if(str.Contains("/*")){//ios error
			bool k = false;
			string tmpstr = "";
			if(str.Contains("/*")){
				for(int j = 0;j < str.Length-1; j++){
					
					if(str[j] == '/' && str[j+1] == '*')
					{
						k = true;
						j++;
					}
					if(str[j] == '*' && str[j+1] == '/')
					{
						k = false;
						j += 1;
						continue;
					}
					if(k == true){
						continue;
					};
					tmpstr += str[j];
				};
				str = tmpstr;
			}

			str = str.Replace("\\n", "");
			str = str.Replace("\\t", "");
			str = str.Replace("\\r", "");
			str = str.Replace("\t", "");
			str = str.Replace("\n", "");
			str = str.Replace("\\", "");
			
			if(str.Contains(" ")){//ios error
				k = false;
				tmpstr = "";
				for(int j = 0;j < str.Length; j++){
					if(!k && str[j] == ' '){
						continue;
					};
					if(str[j] == '"')
						k = !k;
					tmpstr += str[j];
				};
				str = tmpstr;
			}

			//}
			//			Debug.Log("str = " + str);
			int token_tmp = 0;
			if(str.Length > 0) {
				if(str[0] == '"') {
					type = Type.STRING;
					this.str = str.Substring(1, str.Length - 2);
				} else if(str[0] == '{'){
					type = Type.OBJECT;
					keys = new ArrayList();
					list = new ArrayList();
				}else if(str[0] == '('){
					type = xCodeObject.Type.ARRAY;
					list = new ArrayList();
				}else{
					type = Type.STRING;
					this.str = str;
				};
						int depth = 0;
						//bool openquote = false;
						bool inProp = false;
						if(type == Type.OBJECT){
							for(int i = 1; i < str.Length; i++) {
								if( str[i] == '{')
									depth++;
								if(depth == 0) {
									if(str[i] == '=' && !inProp) {
										inProp = true;
										try {
											keys.Add(str.Substring(token_tmp + 1, i - token_tmp - 1));
										} catch { 
											Debug.Log(i + " - " + str.Length + " - " + str); 
										}
										token_tmp = i;
									}
									if((str[i] == ';' || i >= str.Length - 1) && inProp ) {
										inProp = false;
										list.Add(new xCodeObject(str.Substring(token_tmp + 1, i - token_tmp - 1)));

										token_tmp = i;
									}
								}
								if(str[i] == '}')
									depth--;
							}
						}//object
						if(type == Type.ARRAY){
							token_tmp = 0;
							for(int i = 1; i < str.Length; i++) {
								if( str[i] == '(')
									depth++;
								if(depth == 0) {
									if(str[i] == ',') {
										try {
											list.Add(new xCodeObject(str.Substring(token_tmp + 1, i - token_tmp - 1)));
										} catch { 
											Debug.Log(i + " - " + str.Length + " - " + str); 
										}
										token_tmp = i;
									}
								}
								if(str[i] == ')'|| i >= str.Length - 1)
									depth--;
							}
						}//array
			}
		} else {
			type = Type.NULL;	//If the string is missing, this is a null
		}
	}
	public void AddField(bool val) { Add(new xCodeObject(val)); }
	public void AddField(float val) { Add(new xCodeObject(val)); }
	public void AddField(int val) { Add(new xCodeObject(val)); }
	public void Add(xCodeObject obj) {
		if(obj!= null) {		//Don't do anything if the object is null
			if(type != xCodeObject.Type.ARRAY) {
				type = xCodeObject.Type.ARRAY;		//Congratulations, son, you're an ARRAY now
				Debug.LogWarning("tried to add an object to a non-array xCodeObject.  We'll do it for you, but you might be doing something wrong.");
			}
			list.Add(obj);
		}
	}
	public void AddField(string name, bool val) { AddField(name, new xCodeObject(val)); }
	public void AddField(string name, float val) { AddField(name, new xCodeObject(val)); }
	public void AddField(string name, int val) { AddField(name, new xCodeObject(val)); }
	public void AddField(string name, string val) {
		AddField(name, new xCodeObject { type = xCodeObject.Type.STRING, str = val });
	}
	public void AddField(string name, xCodeObject obj) {
		if(obj != null){		//Don't do anything if the object is null
			if(type != xCodeObject.Type.OBJECT){
				type = xCodeObject.Type.OBJECT;		//Congratulations, son, you're an OBJECT now
				Debug.LogWarning("tried to add a field to a non-object xCodeObject.  We'll do it for you, but you might be doing something wrong.");
			}
			keys.Add(name);
			list.Add(obj);
		}
	}
	public void SetField(string name, xCodeObject obj) {
		if(HasField(name)) {
			list.Remove(this[name]);
			keys.Remove(name);
		}
		AddField(name, obj);
	}
	public xCodeObject GetField(string name) {
		if(type == xCodeObject.Type.OBJECT)
			for(int i = 0; i < keys.Count; i++)
				if((string)keys[i] == name)
					return (xCodeObject)list[i];
		return null;
	}
	public bool HasField(string name) {
		if(type == xCodeObject.Type.OBJECT)
			for(int i = 0; i < keys.Count; i++)
				if((string)keys[i] == name)
					return true;
		return false;
	}
	public void Clear() {
		type = xCodeObject.Type.NULL;
		list.Clear();
		keys.Clear();
		str = "";
		n = 0;
		b = false;
	}
	public xCodeObject Copy() {
		return new xCodeObject(print());
	}
	/*
	 * The Merge function is experimental. Use at your own risk.
	 */
	public void Merge(xCodeObject obj) {
		MergeRecur(this, obj);
	}
	static void MergeRecur(xCodeObject left, xCodeObject right) {
		if(right.type == xCodeObject.Type.OBJECT) {
			for(int i = 0; i < right.list.Count; i++) {
				if(right.keys[i] != null) {
					string key = (string)right.keys[i];
					xCodeObject val = (xCodeObject)right.list[i];
					if(val.type == xCodeObject.Type.ARRAY || val.type == xCodeObject.Type.OBJECT) {
						if(left.HasField(key))
							MergeRecur(left[key], val);
						else
							left.AddField(key, val);
					} else {
						if(left.HasField(key))
							left.SetField(key, val);
						else
							left.AddField(key, val);
					}
				}
			}
		}// else left.list.Add(right.list);
	}
	public string print() {
		return print(0);
	}
	public string print(int depth) {	//Convert the xCodeObject into a stiring
		if(depth++ > MAX_DEPTH) {
			Debug.Log("reached max depth!");
			return "";
		}
		string str = "";
		switch(type) {
		case Type.STRING:
			str = this.str;
			break;
		case xCodeObject.Type.OBJECT:
			if(list.Count > 0) {
				str = "{";
				str += "\n";
				depth++;
				for(int i = 0; i < list.Count; i++) {
					string key = (string)keys[i];
					xCodeObject obj = (xCodeObject)list[i];
					if(obj != null) {
						for(int j = 0; j < depth; j++)
							str += "\t"; //for a bit more readability
						str += "" + key + "=";
						str += obj.print(depth) + ";";
						str += "\n";
					}
				}
				str = str.Substring(0, str.Length - 1);
				//str = str.Substring(0, str.Length - 1);
				str += "}";
			} else str += "{}";
			break;
		case xCodeObject.Type.ARRAY:
			if(list.Count > 0) {
				str = "(";
				str += "\n"; //for a bit more readability
				depth++;
				foreach(xCodeObject obj in list) {
					if(obj != null) {
						for(int j = 0; j < depth; j++)
							str += "\t"; //for a bit more readability
						str += obj.print(depth) + ",";
						str += "\n"; //for a bit more readability
					}
				}

				//str = str.Substring(0, str.Length - 1);
				str = str.Substring(0, str.Length - 1);
				str += ")";
			}
			break;
		case Type.NULL:
			str = "null";
			break;
		}
		return str;
	}
	public xCodeObject this[int index] {
		get { return (xCodeObject)list[index]; }
	}
	public xCodeObject this[string index] {
		get { return GetField(index); }
	}
	public override string ToString() {
		return print();
	}
	public Dictionary<string, string> ToDictionary() {
		if(type == Type.OBJECT) {
			Dictionary<string, string> result = new Dictionary<string, string>();
			for(int i = 0; i < list.Count; i++) {
				xCodeObject val = (xCodeObject)list[i];
				switch(val.type){
				case Type.STRING:	result.Add((string)keys[i], val.str);		break;
				case Type.NUMBER:	result.Add((string)keys[i], val.n + "");	break;
				case Type.BOOL:		result.Add((string)keys[i], val.b + "");	break;
				default: Debug.LogWarning("Omitting object: " + (string)keys[i] + " in dictionary conversion"); break;
				}
			}
			return result;
		} else Debug.LogWarning("Tried to turn non-Object xCodeObject into a dictionary");
		return null;
	}
}
