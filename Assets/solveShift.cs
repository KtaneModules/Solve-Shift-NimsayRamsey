using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;
using static ShiftSync;

public class solveShift : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMNeedyModule Needy;
	public KMBossModule BossInfo;
	public KMGameInfo StateCheck;
	public KMGamepad Controller;
	public KMSelectable ModuleSelectable;

	public GameObject ArrowSet;

	public Transform StickShift;
	public KMSelectable[] ShiftArrows;

	public GameObject[] ShiftLayouts;

	public TextMesh DisplayGear;
	public TextMesh DisplayID;

	public Renderer ShifterHead;
	public Material[] ShiftPatterns;

	public bool DebugMode;
	public int DebugBoard;

	//-----------------------------------------------------//
	private int noiseID = 0;
	private int gear = 0;
	private int gearMax = 0;

	private int boardID = 0;
	private int coords = 7;

	private bool syncSelf = false;
	int shiftID;//The module's sequence slot in the group
	int groupID;//The bomb it's on
	private int strikebuffer = 0;

	private float hShift = 0.041f;
	private bool finishSetup = false;

	private bool clutch = false;
	private bool tBypass = false;

	//private int solveCount = 0;
	//-----------------------------------------------------//
	private bool needyActive = false;
	static int moduleIdCounter = 1;
	int moduleId;
	//-----------------------------------------------------//
	
	private void Awake () {
		moduleId = moduleIdCounter++;
		ModuleSelectable.OnFocus += delegate () { ArrowSet.SetActive(true); };
		ModuleSelectable.OnDefocus += delegate () { ArrowSet.SetActive(false); };

		GetComponent<KMNeedyModule>().OnNeedyActivation += NeedyStart;
		//GetComponent<KMNeedyModule>().OnTimerExpired += OnTimerExpired;
		GetComponent<KMNeedyModule>().OnNeedyDeactivation += NeedyOff;

		foreach (KMSelectable NAME in ShiftArrows) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { Shift(pressedObject); return false; };
		}

		StateCheck.OnStateChange += i => { ResetSync(); };
	}

	private void Start () {
		ArrowSet.SetActive(false);

		noiseID = UnityEngine.Random.Range(0, 3);
		StartCoroutine(InitSync());
	}

	IEnumerator InitSync () {
		yield return new WaitForSeconds(0.01f);
		shiftID = DishGroup(Bomb, BossInfo);
		groupID = syncGroup;

		syncSolves[groupID-1] = GrabSolves() + 1;
		setupModule();

		Debug.LogFormat("[Solve Shift #{0}] Shift ID {1} in Group ID {2}", moduleId, shiftID+1, groupID);
		if (shiftID == 0) { Debug.LogFormat("[Solve Shift #{0}] Strike Handler", moduleId); syncMaster[groupID-1] = moduleId; }
		else { Debug.LogFormat("[Solve Shift #{0}] Refer to Shift 1 for Log", moduleId); }
		Debug.LogFormat("[Solve Shift #{0}] Shift ID {1} has a max of {2} gears", syncMaster[groupID-1], shiftID+1, gearMax);
		
		DisplayID.text = (shiftID+1).ToString();

		finishSetup = true;
	}

	private void setupModule() {
		if (DebugMode) { boardID = DebugBoard; } else { boardID = UnityEngine.Random.Range(0, 8); }
		gearMax = BoardMaxList[boardID];
		ShiftLayouts[boardID].SetActive(true);
		if (boardID > 4) {
			hShift = 0.03f;
			if (boardID < 7) { StickShift.transform.localPosition += new Vector3(hShift/2, 0, 0); }
		}

		ShifterHead.material = ShiftPatterns[boardID];

		gearListMax[groupID-1][shiftID] = gearMax;
		totalGearMax[groupID-1] = totalGearMax[groupID-1] * gearMax;

		//Debug.Log(BoardPatterns[boardID, 0] + " " + BoardPatterns[boardID, 1] + " " + BoardPatterns[boardID, 2] + " " + BoardPatterns[boardID, 3] + " " + BoardPatterns[boardID, 4]);
		//Debug.Log(BoardPatterns[boardID, 5] + " " + BoardPatterns[boardID, 6] + " " + BoardPatterns[boardID, 7] + " " + BoardPatterns[boardID, 8] + " " + BoardPatterns[boardID, 9]);
		//Debug.Log(BoardPatterns[boardID, 10] + " " + BoardPatterns[boardID, 11] + " " + BoardPatterns[boardID, 12] + " " + BoardPatterns[boardID, 13] + " " + BoardPatterns[boardID, 14]);
		//Debug.Log(coords);
	}

	void NeedyStart () {
		syncSolves[groupID-1] = GrabSolves() + 1;
		needyActive = true;
	}

	void NeedyOff () {
		needyActive = false;
		CheckGear();
	}

	void Shift (KMSelectable Arrow) {
		int[] sTable = new int[] {-5, 1, 5, -1};
		int direction = Array.IndexOf(ShiftArrows, Arrow);
		switch (direction) {
			case 0:
				if (coords < 5) { return; }
			break;
			case 1:
				if (coords % 5 == 4 || coords < 5 || coords > 9) { return; }
			break;
			case 2:
				if (coords > 9) { return; }
			break;
			case 3:
				if (coords % 5 == 0 || coords < 5 || coords > 9) { return; }
			break;
		}
		direction = sTable[direction];
		if (coords + direction < 0 || coords + direction >= 15 || BoardPatterns[boardID, coords + direction] == 9) { return; }
		
		//Strike if clutch is not engaged
		if (!clutch) {
			Debug.LogFormat("[Solve Shift #{0}] Shift ID {1} attempted to shift gears without clutch engaged. ", syncMaster[groupID-1], shiftID+1, gearMax);
			Needy.HandleStrike();
			return;
		}

		coords = coords + direction;
		if (direction % 5 == 0) {
			if (direction == -5) { direction = 1; } else { direction = -1; }
			StickShift.transform.localPosition += new Vector3(0, 0, 0.042f*direction);
		} else {
			StickShift.transform.localPosition += new Vector3(hShift*direction, 0, 0); //0.03 on 4/5 column
		}
		//Debug.Log(coords);
		gear = BoardPatterns[boardID, coords];
		if (gear == 8) { DisplayGear.text = "R"; gear = 0; }
		else if (coords == 7) { DisplayGear.text = "N"; }
		else if (gear == 0) { DisplayGear.text = " "; }
		else { DisplayGear.text = gear.ToString(); }

		totalGear[groupID-1] = gear;
		gearList[groupID-1][shiftID] = gear;
		syncEveryone = groupID;
		syncSelf = true;
		EndSync(groupID);
		PlaySound();
	}
	
	void Update () {
		if (!finishSetup) { return; }
		clutch = Controller.GetButtonDown(KMGamepad.ButtonEnum.LB) || Controller.GetButtonDown(KMGamepad.ButtonEnum.RB) || Input.GetKey(KeyCode.LeftShift) || tBypass;
		if (syncEveryone == groupID && !syncSelf) {
			totalGear[groupID-1] = totalGear[groupID-1] * gear;
			syncSelf = true;
			EndSync(groupID);
		} else if (syncEveryone != groupID && syncSelf) {
			syncSelf = false;
		}
		//tempTotal.text = totalGear.ToString();
		if (needyActive) {
			Needy.SetNeedyTimeRemaining(syncSolves[groupID-1]);

			if (syncSolves[groupID-1] != GrabSolves() + 1) {
				CheckGear();
				syncSolves[groupID-1] = GrabSolves() + 1;
			}
		}
		if (shiftID == 0 && signalStrike == groupID) {
			strikebuffer++;
			if (strikebuffer == 5) {
				if (!needyActive) { Debug.LogFormat("[Solve Shift #{0}] Right on the last module too...", moduleId); }
				Needy.HandleStrike();
				signalStrike = 0;
				strikebuffer = 0;
			}
		}
	}

	void CheckGear () {
		if (totalGear[groupID-1] == 0) {
			//Debug.Log("Here A");
			if (gear == 0) { Debug.LogFormat("[Solve Shift #{0}] Shift ID {1} was STALLED, in REVERSED, or in NEUTRAL on Display {1}", syncMaster[groupID-1], shiftID+1, syncSolves[groupID-1]); }
			Striker(groupID);
			return;
		} else if (syncSolves[groupID-1] % totalGear[groupID-1] == 0) {
			//Debug.Log("I'm here");
			List<int> j = new List<int> {};
			for (int i = 0; i < gearList[groupID-1].Count; i++) {
				j.Add(gearList[groupID-1][i]);
			}
			for (int i = 0; i < gearListMax[groupID-1].Count; i++) {
				for (int m = i; m < gearListMax[groupID-1].Count; m++) {
					//if (shiftID == 0 ) { Debug.Log("Testing box " + m); }
					//Debug.Log(j[m]);
					while (j[m] < gearListMax[groupID-1][m]) {
						j[m]++;
						int l = 1;
						for (int k = 0; k < gearListMax[groupID-1].Count; k++) {
							l *= j[k];
							//if (shiftID == 0 ) { Debug.Log("Box " + k + " is " + j[k]); }
						}
						//if (shiftID == 0 ) { Debug.Log(l); }
						if (l > syncSolves[groupID-1]) { break; }
						else
						if (syncSolves[groupID-1] % l == 0) {
							if (shiftID == 0) { Debug.LogFormat("[Solve Shift #{0}] Display {1} is divisible by {2}, but could be divided by {3}", moduleId, syncSolves[groupID-1], totalGear[groupID-1], l); }
							Striker(groupID);
							//Debug.Log("Here B");
							return;
						}
					}
					j[i] = gearList[groupID-1][i];
				}
				j[i] = gearListMax[groupID-1][i];
			}
		} else {
			if (shiftID == 0) { Debug.LogFormat("[Solve Shift #{0}] Display {1} was not divisible by {2}", moduleId, syncSolves[groupID-1], totalGear[groupID-1]); }
			Striker(groupID);
			//Debug.Log("Here C");
		}
	}

	private int GrabSolves() {
		int OUT = Bomb.GetSolvedModuleNames().Count();
		foreach (string Boss in Blacklist){
			if (Bomb.GetSolvedModuleNames().Contains(Boss)) {
				OUT--;
			}
		}
		return OUT;
	}

	void PlaySound() {
		string[] sounds = new string[] {
			"Shift1", "Shift2", "Shift3"
		};
		Audio.PlaySoundAtTransform(sounds[noiseID], transform);
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Shift [1-7] -- Shift into the desired gear";
#pragma warning restore 414

	bool isValidPos(string n) {
		string[] valids = { "1", "2", "3", "4", "5", "6", "7" };
		if (!valids.Contains(n)) { return false; }
		return true;
	}

	IEnumerator ProcessTwitchCommand (string command) {
		yield return null;

		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (split[0].EqualsIgnoreCase("SHIFT")) {
			
			int s = 1;
			if (split.Length != 2) {
				yield return "sendtochaterror Please specify a gear to shift into!";
				yield break;
			} else if (!isValidPos(split[1])) {
				yield return "sendtochaterror " + split[1] + " is not a valid gear!";
				yield break;
			} else {
				int.TryParse(split[1], out s);
				if (s > gearMax) {
					yield return "sendtochaterror Shift Box doesn't go that high";
					yield break;
				}
				StartCoroutine(TwitchShift(s));
			}
			yield break;
		}
	}

	void TwitchHandleForcedSolve() { //Autosolver
		//Debug.LogFormat("[Needy Fuse Box #{0}] Autofix software loaded...autOS should fix any future power surges automatically", moduleId);
		Debug.Log("Running autosolver...");
		StartCoroutine(DealWithNeedy());
	}
	
	private int freezeDisplay = 0;

	IEnumerator DealWithNeedy () {
		yield return "sendtochaterror Autosolve will begin once all Solve Shifts on this bomb have activated autosolve...";
		bool solveStart = false;
		while (true) {
			while (!needyActive) {
				yield return null;
			}
			while (!solveStart) {
				twitchAuto[groupID-1][shiftID] = true;
				solveStart = true;
				foreach (bool check in twitchAuto[groupID-1]) {
					if (!check) { solveStart = false; break; }
				}
				yield return null;
			}
			//if (shiftID == 0) { Debug.Log(freezeDisplay + " // " + (GrabSolves()+1)); }
			if (freezeDisplay != GrabSolves()+1) {
				Debug.Log("Solve Count is " + ((GrabSolves()+1)));
				yield return new WaitForSeconds(0.1f);
				List<int> j = new List<int> {};
				List<int> n = new List<int> {};
				for (int i = 0; i < gearListMax[groupID-1].Count; i++) {
					j.Add(1);
					n.Add(1);
				}
				for (int i = 0; i < gearListMax[groupID-1].Count; i++) {
					for (int q = 1; q < gearListMax[groupID-1][i]; q++) {
						for (int m = i; m < gearListMax[groupID-1].Count; m++) {
							if (shiftID == 0 ) { Debug.Log("Testing box " + m); }
							if (shiftID == 0 ) { Debug.Log(gearListMax[groupID-1][m]); }
							while (j[m] < gearListMax[groupID-1][m]) {
								j[m]++;
								int l = 1;
								for (int k = 0; k < gearListMax[groupID-1].Count; k++) {
									l *= j[k];
									if (shiftID == 0 ) { Debug.Log("Box " + k + " is " + j[k]); }
								}
								if (shiftID == 0 ) { Debug.Log(l); }
								//Debug.Log (l > GrabSolves()+1);
								//Debug.Log(GrabSolves()+1);
								if (l > GrabSolves()+1) { break; }
								else
								if ((GrabSolves()+1) % l == 0) {
									if (shiftID == 0 ) { Debug.Log("Found a factor..."); }
									for (int p = 0; p < j.Count; p++){
										n[p] = j[p];
									}
								}
							}
							j[i] = 1;
						}
						j[i] = q;
					}
				}
				Debug.Log("Shifting ID " + shiftID + " to gear " + n[shiftID]);
				StartCoroutine(TwitchShift(n[shiftID]));
				freezeDisplay = GrabSolves()+1;
			}
			yield return new WaitForSeconds(0.1f);
			//yield return null;
		}
	}

	IEnumerator TwitchShift (int tGear) {
		tBypass = true;
		clutch = true;
		if (coords < 5) { ShiftArrows[2].OnInteract(); }
		else if (coords > 9) { ShiftArrows[0].OnInteract(); }
		yield return new WaitForSeconds(0.1f);

		int tLoc = 0;
		for (int i = 0; i < 15; i++) {
			if (BoardPatterns[boardID, i] == tGear) {
				tLoc = i;
				break;
			}
		}

		while (gear != tGear) {
			if (coords % 5 != tLoc % 5) {
				if (coords % 5 < tLoc % 5) { ShiftArrows[1].OnInteract(); } else { ShiftArrows[3].OnInteract(); }
			} else {
				if (tLoc < 5) { ShiftArrows[0].OnInteract(); } else { ShiftArrows[2].OnInteract(); }
			}
			yield return new WaitForSeconds(0.1f);
		}
		tBypass = false;
		yield return null;
	}

}