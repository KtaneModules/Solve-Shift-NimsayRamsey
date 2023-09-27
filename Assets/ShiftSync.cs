using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;

public static class ShiftSync {
	
	public static List<int> syncMaster = new List<int> {};

	public static List<int> totalGear = new List<int> {};
	public static List<int> totalGearMax = new List<int> {};

	public static List<int> syncSolves = new List<int> {};

	public static List<List<int>> gearList = new List<List<int>> {};
	public static List<List<int>> gearListMax = new List<List<int>> {};

	public static List<List<bool>> twitchAuto = new List<List<bool>> {};

	//Ran at the start of the mission
	public static int syncGroup = 0;

	private static string groupSerial = "";
	private static int ID = 0;

	public static List<string> Blacklist = new List<string> {};
	private static bool banGrabbed = false;

	public static int DishGroup (KMBombInfo Bomb, KMBossModule Boss) {
		//Run once, never run again until the game is closed
		if (!banGrabbed) {
			Blacklist = Boss.GetIgnoredModules("Solve Shift").ToList();
			//REMOVE ONCE JSON IS ACCEPTED
			Blacklist.Add("Solve Shift");
			//END

			banGrabbed = true;
		}
		//------------------------------------------------//

		if (groupSerial == Bomb.GetSerialNumber()) { ID++; }
		else {
			groupSerial = Bomb.GetSerialNumber();
			ID = 0;
			syncGroup++;
			syncCount.Add(0);
			syncMax.Add(0);
			gearList.Add(new List<int> {});
			gearListMax.Add(new List<int> {});
			twitchAuto.Add(new List<bool> {});
			totalGear.Add(0);
			totalGearMax.Add(1);
			syncSolves.Add(0);
			syncMaster.Add(0);
		}
		syncMax[syncGroup-1]++;
		gearList[syncGroup-1].Add(0);
		gearListMax[syncGroup-1].Add(0);
		twitchAuto[syncGroup-1].Add(false);
		return ID;
	}

	// Runs when syncing
	public static int syncEveryone = 0;

	private static List<int> syncCount = new List<int> {};
	private static List<int> syncMax = new List<int> {};

	public static void EndSync (int set) {
		syncCount[set-1]++;
		if (syncCount[set-1] == syncMax[set-1]) {
			syncEveryone = 0;
			syncCount[set-1] = 0;
		}
	}

	// Runs when a strike is ordered
	public static int signalStrike = 0;

	public static void Striker(int set) {
		signalStrike = set;
	}

	// Ran when the room loads in/out
	public static void ResetSync () {
		syncMaster = new List<int> {};
		totalGear = new List<int> {};
		totalGearMax = new List<int> {};
		syncEveryone = 0;
		syncCount = new List<int> {};
		syncMax = new List<int> {};

		syncSolves = new List<int> {};

		groupSerial = "";
		syncGroup = 0;
		ID = -1;

		gearList = new List<List<int>> {};
		gearListMax = new List<List<int>> {};
		twitchAuto = new List<List<bool>> {};
	}

	public static int[] BoardMaxList = new int[] {3, 4, 4, 5, 5, 5, 6, 7};

	public static int[,] BoardPatterns = new int[,] {
		{//3 shift
			9, 8, 9, 2, 9,
			9, 0, 0, 0, 9,
			9, 1, 9, 3, 9
		},
		{//4 shift pre 1967
			9, 8, 2, 4, 9,
			9, 0, 0, 0, 9,
			9, 1, 3, 9, 9
		},
		{//4 shift post 1967
			9, 1, 3, 9, 9,
			9, 0, 0, 0, 9,
			9, 2, 4, 8, 9
		},
		{//5 shift standard
			9, 1, 3, 5, 9,
			9, 0, 0, 0, 9,
			9, 2, 4, 8, 9
		},
		{//5 shift dog
			9, 8, 2, 4, 9,
			9, 0, 0, 0, 9,
			9, 1, 3, 5, 9
		},
		{//5 shift Reverse
			8, 1, 3, 5, 9,
			0, 0, 0, 0, 9,
			9, 2, 4, 9, 9
		},
		{//6 shift
			8, 1, 3, 5, 9,
			0, 0, 0, 0, 9,
			9, 2, 4, 6, 9
		},
		{//7 shift
			8, 1, 3, 5, 7,
			0, 0, 0, 0, 0,
			9, 2, 4, 6, 9
		}
	};

}
