
#include "stdafx.h"
#define _WINSOCK_DEPRECATED_NO_WARNINGS 
#include<stdio.h>
#include<winsock2.h>
#include <string>
#include <iostream>
#include <sstream>
#include <fstream>
#include <chrono>
#include <vector>
#include <thread>
#include <map>
#include <sys/types.h>
#include <algorithm>

#pragma comment(lib,"ws2_32.lib") //Winsock Library

#define BUFLEN 1024  //Max length of buffer
#define PORT 8484   //The port on which to listen for incoming data
using namespace std;

map<string, vector<string>>	serverlist;  // [servername] -> ([lobbyname],[lobbyname],[lobbyname])
map<string, vector<string>> openlobby;   // [servername] -> ([ip:port],[ip:port],[ip:port]) of servers without games running
map<string, string>			lobbyport;   // [servername:lobbyname] -> [ip:port]
map<string, vector<string>> lobbyinfo;   // [servername:lobbyname] -> ([maxplayers,currentplayers,etc])
map<string, vector<string>> playerlist;  // [servername:lobbyname] -> [([SteamID, SteamID, SteamID])]
map<string, string>			currentgame; // [SteamID] -> [servername:lobbyname]
map<string, string>			playeraddrs; // [SteamID] -> [ip:port]

map<string, vector<string>>::iterator slit = serverlist.begin();
map<string, vector<string>>::iterator olit = openlobby.begin();
map<string, string>::iterator         lpit = lobbyport.begin();
map<string, vector<string>>::iterator liit = lobbyinfo.begin();
map<string, vector<string>>::iterator plit = playerlist.begin();
map<string, string>::iterator         cgit = currentgame.begin();
map<string, string>::iterator		  pait = playeraddrs.begin();

void printMaps();
void closeMaps();

int main()
{
	SOCKET s;
	struct sockaddr_in server, si_other;
	int slen, recv_len;
	char buf[BUFLEN];
	WSADATA wsa;

	slen = sizeof(si_other);

	//Initialise winsock
	//printf("\nInitialising Winsock...");
	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
	{
		printf("Failed. Error Code : %d", WSAGetLastError());
		exit(EXIT_FAILURE);
	}
	//printf("Initialised.\n");

	//Create a socket
	if ((s = socket(AF_INET, SOCK_DGRAM, 0)) == INVALID_SOCKET)
	{
		printf("Could not create socket : %d", WSAGetLastError());
	}
	//printf("Socket created.\n");

	//Prepare the sockaddr_in structure
	server.sin_family = AF_INET;
	server.sin_addr.s_addr = INADDR_ANY;
	server.sin_port = htons(PORT);

	//Bind
	if (::bind(s, (struct sockaddr *)&server, sizeof(server)) == SOCKET_ERROR)
	{
		printf("Bind failed with error code : %d", WSAGetLastError());
		exit(EXIT_FAILURE);
	}
	//puts("Bind done");
	cout << "Opened on port " << to_string((int)ntohs(server.sin_port)) << endl;
	//keep listening for data
	while (1)
	{
		printf("Master server running...\n");
		fflush(stdout);
		memset(buf, '\0', BUFLEN);
		if ((recv_len = recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen)) == SOCKET_ERROR)
		{
			printf("recvfrom() failed with error code : %d", WSAGetLastError());
			exit(EXIT_FAILURE);
		}
		// packet received from address:inet_ntoa(si_other.sin_addr)
		// packet received from    port:ntohs(si_other.sin_port)
		// packet recived  is in buf as string

		//print details of the client/peer and the data received
		// com = command issued in packet, arg = argument provided in packet
		string com = (string)buf; com = com.substr(0, 5);
		string arg = (string)(buf + 6);
		cout << com << endl;
		cout << arg << endl;
		printf("-- %s --\n",buf);

		// s = sender
		// get sender's information
		string saddr, sport, saddrport, regionname, tmp, tosend;
		saddr = inet_ntoa(si_other.sin_addr);
		sport = to_string((int)ntohs(si_other.sin_port));
		saddrport = saddr + ":" + sport;

		// CASE: Register server
		// USE:  stser [servername]
		if (com == "stser") {
			/*
			1) Look if server is in serverlist already
			- If not, create vector with ip:port and add to openlobby for [servername],
			create empty entry in serverlist for [servername]
			- If so, add ip:port to vector in entry for openlobby for [servername]
			*/

			// get server name from packet
			stringstream ss(arg);
			getline(ss, tmp, ':');
			regionname = tmp;

			// bad request (no region provided)
			if (regionname == "") {
				cout << "BAD REQUEST\n";
				continue;
			}

			// register server with IP:port of whoever sent packet
			if (serverlist.count(regionname) == 0 && serverlist.find(regionname) == serverlist.end()) {
				// if this is a new server
				vector<string> v;
				v.push_back(saddrport);
				serverlist[regionname];
				openlobby[regionname] = v;
			}
			else {
				// if server is in serverlist
				openlobby[regionname].push_back(saddrport);
			}

			// 2) send ACK for stser back to sender/server
			// send new lobby its info
			tosend = "ssack " + regionname;
			cout << "sending " << tosend << " to " << saddrport << endl;
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");
		}
		// CASE: Register lobby
		// USE:  stlob [servername:lobbyname]
		else if (com == "stlob") {
			/*
			1) If servername exists and lobbyname not taken, Get an ip:port from openlobby[servername] and register a lobby for it
			- remove ip:port from openlobby[servername]
			- add lobbyname to serverlist[servername]
			- add ip:port to lobbyport[servername:lobbyname]
			- add empty vector to lobbyinfo[servername:lobbyname]
			- add empty vector to playerlist[servername:lobbyname]
			2) Send lobby info to allocated ip:port

			1) receive packet from client
			2) send packet to server
			3) receive ack from server
			4) send ack to server
			
			*/

			// get servername:lobbyname from packet
			string lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			regionname = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = regionname + ":" + lname;

			// bad request (no servername or lobbyname, or servername has not been registered)
			if (regionname == "" || lname == "" || serverlist.count(regionname) == 0) {
				cout << "BAD REQUEST\n";
				continue;
			}
			// bad request (lobbyname has been taken for that servername)
			if (serverlist[regionname].size() > 1 && find(serverlist[regionname].begin(), serverlist[regionname].end(), lname) == serverlist[regionname].end())
			{
				cout << "BAD REQUEST lobby already opened\n";
				continue;
			}

			// register lobby if there is a lobby available
			if (openlobby[regionname].size() > 0) {
				// get new lobby, send new lobby its info
				string addrport = openlobby[regionname].back();
				string laddr = addrport.substr(0, addrport.find(":"));
				string lport = addrport.substr(addrport.find(":") + 1, arg.length() - arg.find(":"));
				tosend = "stlob " + slname;
				cout << "sending " << tosend << " to " << laddr << ":" << lport << endl;
				si_other.sin_port = htons(atoi(lport.c_str()));
				si_other.sin_addr.S_un.S_addr = inet_addr(laddr.c_str());
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");

				// get ACK(slack) from new lobby
				memset(buf, '\0', BUFLEN);
				if ((recv_len = recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen)) == SOCKET_ERROR)
				{
					printf("recvfrom() failed with error code : %d", WSAGetLastError());
					exit(EXIT_FAILURE);
				}
				com = (string)buf; com = com.substr(0, 5);
				arg = (string)(buf + 6);
				// if we didnt get the ACK, buffer what you received (NYI)
				// if we get the ACK, send ACK to client
				if (com != "slack") {
					//buffer the packet
				}
				else {
					// save the new lobby
					openlobby[regionname].pop_back();
					serverlist[regionname].push_back(lname);
					lobbyport[slname] = addrport;
					lobbyinfo[slname];
					playerlist[slname];
					// build ACK
					tosend = "slack " + slname;
					cout << "sending " << tosend << " to " << saddr << ":" << sport << endl;
					si_other.sin_port = htons(atoi(sport.c_str()));
					si_other.sin_addr.S_un.S_addr = inet_addr(saddr.c_str());
					// send ACK to client
					int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
					if (n < 0) perror("sendto");
				}
			}

		}
		// CASE: Unregister lobby/server
		// USE:  close [servername:lobbyname]
		else if (com == "close") {
			//regionname -> servername to close lobby on
			//lname -> lobbyname  to close
			//saddrport -> ip:port of who has requested to close

			// get servername:lobbyname from packet
			string lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			regionname = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = regionname + ":" + lname;

			// bad request (no servername or lobbyname, or servername has not been registered)
			if (regionname == "" || lname == "" || serverlist.count(regionname) == 0) {
				cout << "BAD REQUEST nothing provided or servername\n";
				continue;
			}
			// bad request (lobbyname has not been taken for that servername)
			if (serverlist[regionname].size() > 1 && find(serverlist[regionname].begin(), serverlist[regionname].end(), lname) != serverlist[regionname].end())
			{
				cout << "BAD REQUEST lobbyname\n";
				continue;
			}

			/*
			1) remove lobbyname from serverlist[servername]
			2) delete lobbyport[servername:lobbyname]
			3) delete lobbyinfo[servername:lobbyname]
			4) for each player in vector playerlist[servername:lobbyname], delete currentgame[player]
			5) delete playerlist[servername:lobbyname]
			*/

			if (serverlist[regionname].size() > 1) {
				serverlist[regionname].erase(find(serverlist[regionname].begin(), serverlist[regionname].end(), lname));
			}
			else {
				serverlist.erase(regionname);
			}
			if (openlobby[regionname].size() == 0) {
				openlobby.erase(regionname);
			}
			lobbyport.erase(slname);
			lobbyinfo.erase(slname);
			for (int i = 0; i < playerlist[slname].size(); i++) {
				currentgame.erase(playerlist[slname][i]);
			}
			playerlist.erase(slname);

			tosend = "clack " + slname;
			cout << "sending " << tosend << " to " << saddr << ":" << sport << endl;
			// send ACK back to client
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");

		}
		// CASE: update lobby playerlist
		// USE:  lobup [servername:lobbyname]:[player1]:[player2]...
		else if (com == "lobup") {
			//regionname -> servername to update
			//lname -> lobbyname  to update
			//saddrport -> ip:port of server to update

			// get servername:lobbyname from packet
			string lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			regionname = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = regionname + ":" + lname;

			//now have servername, lobbyname, saddr, sport, and saddrport
			/*
			- create empty vector
			- for each SteamID following lobbyname
			- update currentgame[SteamID] with [servername:lobbyname]
			- add to vector
			- map vector to playerlist[servername:lobbyname]
			*/
			vector<string> v;
			string player = "";
			while (1) {
				getline(ss, tmp, ':');
				player = tmp;
				if (player == "") {
					break;
				}

				currentgame[player] = slname;
				v.push_back(player);
			}
			playerlist[slname] = v;
		}
		// CASE: player get list of servers
		// USE:  pslis [username]
		else if (com == "pslis") {
			string uname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			uname = tmp;
			if (arg != "") {
				playeraddrs[uname] = saddrport;
			}
			tosend = "psack ";

			//build output list of servers
			for (slit = serverlist.begin(); slit != serverlist.end(); ++slit) {
				tosend += slit->first;
				if (++slit != serverlist.end())
					tosend += ":";
				slit--;
			}

			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");
		}
		// CASE: player get list of lobbies open on server
		// USE:  pllis [servername]
		else if (com == "pllis") {
			//regionname -> servername to register this ip:port for
			//saddrport -> ip:port of server that has requested to be registered

			// get servername from packet
			stringstream ss(arg);
			getline(ss, tmp, ':');
			regionname = tmp;

			// bad request (no servername)
			if (regionname == "") {
				cout << "BAD REQUEST\n";
				continue;
			}

			tosend = "plack ";
			vector<string> v = serverlist[regionname];
			//build output list of servers
			for (int i = 0; i < v.size(); ++i) {
				tosend += v[i];
				if(i+1 != v.size())
					tosend += ":";
			}

			//send plack region:lobby1:lobby2 back to the client
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");
		}
		// CASE: player join lobby (sent by player)
		// USE:  pjoin [username:servername:lobbyname]
		else if (com == "pjoin") {
			//uname -> username of who is joining this server
			//regionname -> servername that lobby is hosted on
			//lname -> lobbyname of lobby
			//saddrport -> ip:port of user that made this request
			string uname, lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			uname = tmp;
			getline(ss, tmp, ':');
			regionname = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = regionname + ":" + lname;

			string playeraddr = saddr;
			string playerport = sport;

			// bad request (no servername or lobbyname, or servername has not been registered)
			if (regionname == "" || lname == "" || serverlist.count(regionname) == 0) {
				cout << "BAD REQUEST nothing provided or bad servername\n";
				continue;
			}
			// bad request (lobbyname has not been taken for that servername)
			if (lobbyinfo.count(slname) == 0)
			{
				cout << "BAD REQUEST bad lobbyname\n";
				continue;
			}

			/*
			- add ip:port for user to playeraddrs
			- get ip:port of servername:lobbyname
			1) add user to playerlist[servername:lobbyname]
			2) add servername:lobbyname to currentgame[SteamID]
			3) send user [ip:port]
			*/

			playeraddrs[uname] = saddrport;
			playerlist[slname].push_back(uname);
			currentgame[uname] = slname;
			saddrport = lobbyport[slname];
			tosend = "pjoin " + uname + ":" + slname;
			string serveraddr = saddrport.substr(0, saddrport.find(":"));
			string serverport = saddrport.substr(saddrport.find(":") + 1, saddrport.length() - saddrport.find(":"));
			si_other.sin_port = htons(atoi(serverport.c_str()));
			si_other.sin_addr.S_un.S_addr = inet_addr(serveraddr.c_str());
			// send pjoin SteamID:region:lobby to server
			cout << "sending " << tosend << " to " << serveraddr << ":" << serverport << endl;
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");

			// receive pjack SteamID:region:lobby from server
			memset(buf, '\0', BUFLEN);
			if ((recv_len = recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen)) == SOCKET_ERROR)
			{
				printf("recvfrom() failed with error code : %d", WSAGetLastError());
				exit(EXIT_FAILURE);
			}
			com = (string)buf; com = com.substr(0, 5);
			arg = (string)(buf + 6);
			// if we didnt get the ACK, buffer what you received (NYI)
			// if we get the ACK, send ACK to client
			if (com != "pjack") {
				//buffer the packet
			}
			else {
				stringstream ss(arg);
				getline(ss, tmp, ':');
				uname = tmp;
				getline(ss, tmp, ':');
				regionname = tmp;
				getline(ss, tmp, ':');
				lname = tmp;
				slname = regionname + ":" + lname;
				tosend = "pjack " + uname + ":" + lobbyport[slname];
				cout << "sending " << tosend << " to " << playerport << ":" << playeraddr << endl;
				si_other.sin_port = htons(atoi(playerport.c_str()));
				si_other.sin_addr.S_un.S_addr = inet_addr(playeraddr.c_str());
				// send pjack SteamID:IP:port to specified user
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
		}
		// CASE: player quit lobby (masterserver removes specified player from playerlist of server theyre connected to)
		// USE:  pquit [username]
		else if (com == "pquit") {
			//uname -> steamID of who has quit their lobby
			//saddrport -> ip:port of server player is removed from

			// get username from packet
			string uname, tmp;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			uname = tmp;

			// bad request (no servername)
			if (uname == "") {
				cout << "BAD REQUEST\n";
				continue;
			}

			/*
			1) get servername:lobbyname from currentgame[uname]
			2) remove uname from playerlist[servername:lobbyname]
			3) delete currentgame[uname]
			*/
			string slname = currentgame[uname];
			if (playerlist[slname].size() > 1) {
				playerlist[slname].erase(find(playerlist[slname].begin(), playerlist[slname].end(), uname));
			}
			else {
				playerlist.erase(slname);
			}
			currentgame.erase(uname);

			// send ACK back to client
			tosend = "pqack " + uname;
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");
		}
		// cASE: player invites another player to their current game
		// USE: pinvi [fromusername]:[tousername]
		else if (com == "pinvi") {
			string fromname, toname, slname, uaddr, uport, uaddrport, str;
			fromname = arg.substr(0, arg.find(":"));
			toname = arg.substr(arg.find(":") + 1, arg.length() - arg.find(":"));

			string playeraddr = saddr;
			string playerport = sport;

			//player == inviter
			//u      == invited
			//slname == region:lobby of game

			// find specified players current game (servername:lobbyname) and ip:port of who to send it to
			slname = currentgame[fromname];
			printf("slname done\n");
			uaddrport = playeraddrs[toname];
			printf("uaddrport done\n");
			uaddr = uaddrport.substr(0, uaddrport.find(":"));
			uport = uaddrport.substr(uaddrport.find(":") + 1, arg.length() - arg.find(":"));

			// send that to the remembered IP:port of invited player

			tosend = "pinvi " + fromname + ":" + toname;
			cout << "sending " << tosend << " to " << uaddr << ":" << uport << endl;
			si_other.sin_port = htons(atoi(uport.c_str()));
			si_other.sin_addr.S_un.S_addr = inet_addr(uaddr.c_str());
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");

			// receive piack FromSteamID:ToSteamID from invited
			memset(buf, '\0', BUFLEN);
			if ((recv_len = recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen)) == SOCKET_ERROR)
			{
				printf("recvfrom() failed with error code : %d", WSAGetLastError());
				exit(EXIT_FAILURE);
			}
			com = (string)buf; com = com.substr(0, 5);
			arg = (string)(buf + 6);
			// if we didnt get the ACK, buffer what you received (NYI)
			// if we get the ACK, send ACK to client
			if (com != "piack") {
				//buffer the packet
			}
			else {
				//tosend = "piack " + fromname + ":" + toname + ":" + slname;
				tosend = "piack " + fromname + ":" + toname;
				cout << "sending " << tosend << " to " << saddr << ":" << sport << endl;
				si_other.sin_port = htons(atoi(playerport.c_str()));
				si_other.sin_addr.S_un.S_addr = inet_addr(playeraddr.c_str());
				// send piack SteamID:IP:port to specified user
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
		}
		else {
			cout << "!! - INCORRECT INPUT - !!" << endl;
		}


		printMaps();
		printf("-------------\n");
	}

	closeMaps();
	closesocket(s);
	WSACleanup();

	return 0;
}

void printMaps() {
	cout << "- serverlist contains:\n";
	for (slit = serverlist.begin(); slit != serverlist.end(); ++slit) {
		cout << slit->first << " => ";
		for (int i = 0; i<slit->second.size(); ++i)
			cout << slit->second[i] << ' ';
		cout << endl;
	}
	cout << "- openlobby contains:\n";
	for (olit = openlobby.begin(); olit != openlobby.end(); ++olit) {
		cout << olit->first << " => ";
		for (int i = 0; i<olit->second.size(); ++i)
			cout << olit->second[i] << ' ';
		cout << endl;
	}
	cout << "- lobbyport contains:\n";
	for (lpit = lobbyport.begin(); lpit != lobbyport.end(); ++lpit) {
		cout << lpit->first << " => " << lpit->second << endl;
	}
	cout << "- lobbyinfo contains:\n";
	for (liit = lobbyinfo.begin(); liit != lobbyinfo.end(); ++liit) {
		cout << liit->first << " => ";
		for (int i = 0; i<liit->second.size(); ++i)
			cout << liit->second[i] << ' ';
		cout << endl;
	}
	cout << "- playerlist contains:\n";
	for (plit = playerlist.begin(); plit != playerlist.end(); ++plit) {
		cout << plit->first << " => ";
		for (int i = 0; i<plit->second.size(); ++i)
			cout << plit->second[i] << ' ';
		cout << endl;
	}
	cout << "- currentgame contains:\n";
	for (cgit = currentgame.begin(); cgit != currentgame.end(); ++cgit) {
		cout << cgit->first << " => " << cgit->second << endl;
	}
	cout << "- playeraddrs contains:\n";
	for (pait = playeraddrs.begin(); pait != playeraddrs.end(); ++pait) {
		cout << pait->first << " => " << pait->second << endl;
	}
}

void closeMaps() {
	for (slit = serverlist.begin(); slit != serverlist.end(); ++slit) {
		serverlist.erase(slit->first);
	}
	for (olit = openlobby.begin(); olit != openlobby.end(); ++olit) {
		openlobby.erase(olit->first);
	}
	for (lpit = lobbyport.begin(); lpit != lobbyport.end(); ++lpit) {
		lobbyport.erase(lpit->first);
	}
	for (liit = lobbyinfo.begin(); liit != lobbyinfo.end(); ++liit) {
		lobbyinfo.erase(liit->first);
	}
	for (plit = playerlist.begin(); plit != playerlist.end(); ++plit) {
		playerlist.erase(plit->first);
	}
	for (cgit = currentgame.begin(); cgit != currentgame.end(); ++cgit) {
		currentgame.erase(cgit->first);
	}
	for (pait = playeraddrs.begin(); pait != playeraddrs.end(); ++pait) {
		playeraddrs.erase(pait->first);
	}
}