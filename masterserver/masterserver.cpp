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

struct Packet {
	string command;  // [command]
	string arguments;// [arg1]:[arg2]:[arg3]
	string fromaddr; // IP address of sender
	string fromport; // port of sender
	string toaddr;   // IP address of receiver
	string toport;   // port of receiver
	chrono::system_clock::time_point timestamp;
};
vector<Packet> unackedPackets;

map<string, vector<string>>	serverlist;  // [region] -> ([lobbyname],[lobbyname],[lobbyname])
map<string, vector<string>> openlobby;   // [region] -> ([ip:port],[ip:port],[ip:port]) of servers without games running
map<string, string>			lobbyport;   // [region:lobbyname] -> [ip:port]
map<string, vector<string>> lobbyinfo;   // [region:lobbyname] -> ([maxplayers,currentplayers,etc])
map<string, vector<string>> playerlist;  // [region:lobbyname] -> [([SteamID, SteamID, SteamID])]
map<string, string>			currentgame; // [SteamID] -> [region:lobbyname]
map<string, string>			playeraddrs; // [SteamID] -> [ip:port]

map<string, vector<string>>::iterator slit = serverlist.begin();
map<string, vector<string>>::iterator olit = openlobby.begin();
map<string, string>::iterator         lpit = lobbyport.begin();
map<string, vector<string>>::iterator liit = lobbyinfo.begin();
map<string, vector<string>>::iterator plit = playerlist.begin();
map<string, string>::iterator         cgit = currentgame.begin();
map<string, string>::iterator		  pait = playeraddrs.begin();

void retransmitunACKed();
void printunACKed();
void printMaps();
void closeMaps();

//RTO(X), where X is time before retransmitting packets, in milliseconds
chrono::duration<int, ratio<1, 1000>> RTO(500);
// initialize socket data
SOCKET s;
struct sockaddr_in server, si_other;
int slen, recv_len;
char buf[BUFLEN];
WSADATA wsa;

int main()
{
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

	//set socket to non-blocking
	u_long mode = 1;
	int nonblock = ioctlsocket(s, FIONBIO, &mode);
	if (nonblock != NO_ERROR)
		printf("ioctlsocket failed with error: %ld\n", nonblock);

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
	// END OF BOILERPLATE SOCKET CODE

	printf("Master server started...\n");
	// server loop
	while (1)
	{
		fflush(stdout);

		//
		// RECEIVE PACKET
		//
		// clear receive buffer, receive packet
		
		memset(buf, '\0', BUFLEN);
		if ((recv_len = recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen)) == SOCKET_ERROR)
		{
			int err = WSAGetLastError();
			if (err != WSAEWOULDBLOCK) {
				printf("recvfrom() failed with error code : %d", err);
				exit(EXIT_FAILURE);
			}
			// if there is nothing in buffer, just take care of unACKed packets
			retransmitunACKed();
			continue;
		}
		buf[recv_len] = '\0';
		
		// packet received from address:inet_ntoa(si_other.sin_addr)
		// packet received from    port:ntohs(si_other.sin_port)
		// packet recived  is in buf as string

		// parse packet details
		// com = command issued in packet, arg = argument provided in packet
		string com = (string)buf; com = com.substr(0, 5);
		string arg = (string)(buf + 6);
		// put in Packet struct
		Packet p;
		p.command = com;
		p.arguments = arg;
		p.fromaddr = inet_ntoa(si_other.sin_addr);
		p.fromport = to_string((int)ntohs(si_other.sin_port));
		p.timestamp = chrono::system_clock::now();
		string saddrport = p.fromaddr + ":" + p.fromport;
		string region, tmp, tosend;

		//
		// PARSE PACKET
		//

		// CASE: Register server
		// USE:  stser [region]
		if (com == "stser") {
			// get server name from packet
			stringstream ss(arg);
			getline(ss, tmp, ':');
			region = tmp;

			// bad request (no region provided)
			if (region == "") {
				cout << "BAD REQUEST\n";
				continue;
			}

			/*
			1) Look if server is in serverlist already
			- If not, create vector with ip:port and add to openlobby for [region],
			create empty entry in serverlist for [region]
			- If so, if not already an open lobby or lobbyport entry added add ip:port to vector in entry for openlobby for [region] 
			*/

			// register server with IP:port of whoever sent packet
			if (serverlist.count(region) == 0 && serverlist.find(region) == serverlist.end()) {
				// if this is a new server
				vector<string> v;
				v.push_back(saddrport);
				serverlist[region];
				openlobby[region] = v;
			}
			else {
				// if this is an old server
				if (openlobby[region].size() > 1 && find(openlobby[region].begin(), openlobby[region].end(), saddrport) != openlobby[region].end()){
					// if this lobby is already an openlobby
					continue;
				}
				else {
					// if this is a new lobby
					openlobby[region].push_back(saddrport);
				}
			}

			// 2) send ACK for stser back to sender/server
			// send new lobby its info
			tosend = "ssack " + region;
			cout << "sending " << tosend << " to " << saddrport << endl;
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");
		}
		// CASE: Register lobby
		// USE:  stlob region:lobby
		else if (com == "stlob") {
			/*
			1) If region exists and lobbyname not taken, Get an ip:port from openlobby[region] and register a lobby for it
			- remove ip:port from openlobby[region]
			- add lobbyname to serverlist[region]
			- add ip:port to lobbyport[region:lobbyname]
			- add empty vector to lobbyinfo[region:lobbyname]
			- add empty vector to playerlist[region:lobbyname]
			2) Send lobby info to allocated ip:port

			1) receive packet from client
			2) send packet to server
			3) receive ack from server
			4) send ack to server

			1) 
			*/

			// get region:lobbyname from packet
			string lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			region = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = region + ":" + lname;

			// bad request (no region or lobbyname, or region has not been registered)
			if (region == "" || lname == "" || serverlist.count(region) == 0) {
				cout << "BAD REQUEST\n";
			}
			// redundant request (if already taken  OR UNACKED should send slack back to client)
			else if (lobbyport.count(slname) > 0) {
				// send slack back to client as if it was just opened
				tosend = "slack " + slname;
				cout << "sending " << tosend << " to " << p.fromaddr << ":" << p.fromport << endl;
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			} 
			// register lobby if there is a lobby available
			else if (openlobby[region].size() > 0) {
				// get new lobby
				string addrport = openlobby[region].back();
				string laddr = addrport.substr(0, addrport.find(":"));
				string lport = addrport.substr(addrport.find(":") + 1, arg.length() - arg.find(":"));
				openlobby[region].pop_back();
				// save unACKed packet
				p.toaddr = laddr;
				p.toport = lport;
				unackedPackets.push_back(p);
				// send new lobby its info
				tosend = "stlob " + slname;
				cout << "sending " << tosend << " to " << laddr << ":" << lport << endl;
				si_other.sin_port = htons(atoi(lport.c_str()));
				si_other.sin_addr.S_un.S_addr = inet_addr(laddr.c_str());
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
		}
		// CASE: Receive stlob ACK from server, register lobby
		// USE: slack region:lobby
		else if (com == "slack") {
			// get region:lobby from packet
			string lname, slname, returnaddr, returnport;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			region = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = region + ":" + lname;

			// bad request (no region or lobbyname, or region has not been registered)
			if (region == "" || lname == "" || serverlist.count(region) == 0) {
				cout << "BAD REQUEST\n";
				continue;
			}

			// check if there is an unACKed packet for this (stlob region:lobby)
			for (int i = 0; i < unackedPackets.size(); ++i) {
				// check if unACKed packet matches
				if (unackedPackets[i].command == "stlob" && unackedPackets[i].arguments == arg) {
					// save the new lobby
					serverlist[region].push_back(lname);
					lobbyport[slname] = p.fromaddr + ":" + p.fromport;
					lobbyinfo[slname];
					playerlist[slname];
					returnaddr = unackedPackets[i].fromaddr;
					returnport = unackedPackets[i].fromport;
					// remove unACKed packet from list
					unackedPackets.erase(unackedPackets.begin() + i);
					// build ACK to send back to client starting the lobby
					tosend = "slack " + slname;
					cout << "sending " << tosend << " to " << returnaddr << ":" << returnport << endl;
					si_other.sin_port = htons(atoi(returnport.c_str()));
					si_other.sin_addr.S_un.S_addr = inet_addr(returnaddr.c_str());
					// send ACK to client
					int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
					if (n < 0) perror("sendto");
				}
			}
		}
		// CASE: Unregister lobby/server
		// USE:  close region:lobby
		else if (com == "close") {
			//region -> region to close lobby on
			//lname -> lobbyname  to close
			//saddrport -> ip:port of who has requested to close

			// get region:lobbyname from packet
			string lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			region = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = region + ":" + lname;

			// bad request (no region or lobbyname, or region has not been registered)
			if (region == "" || lname == "") {
				cout << "BAD REQUEST missing region or lobbyname\n";
				continue;
			}
			// redundant request (region doesn't exist, region:lobby doesn't exist
			if (serverlist.count(region) == 0 || lobbyport.count(slname)){
				cout << "BAD REQUEST lobbyname\n";
				// send "clack region:lobby back to sender"
				tosend = "clack " + slname;
				cout << "sending " << tosend << " to " << p.fromaddr << ":" << p.fromport << endl;
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			} 
			// valid request
			else { 
				/*
				1) remove lobbyname from serverlist[servername]
				2) delete lobbyport[region:lobbyname]
				3) delete lobbyinfo[region:lobbyname]
				4) for each player in vector playerlist[region:lobbyname], delete currentgame[player]
				5) delete playerlist[region:lobbyname]
				*/
				if (serverlist[region].size() > 1) {
					serverlist[region].erase(find(serverlist[region].begin(), serverlist[region].end(), lname));
				}
				else {
					serverlist.erase(region);
				}
				if (openlobby[region].size() == 0) {
					openlobby.erase(region);
				}
				lobbyport.erase(slname);
				lobbyinfo.erase(slname);
				for (int i = 0; i < playerlist[slname].size(); i++) {
					currentgame.erase(playerlist[slname][i]);
				}
				playerlist.erase(slname);

				// send ACK back to sender(client)
				tosend = "clack " + slname;
				cout << "sending " << tosend << " to " << p.fromaddr << ":" << p.fromport << endl;
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}

		}
		// CASE: update lobby playerlist
		// USE:  lobup region:lobby:player1:player2:player3 (not used)
		else if (com == "lobup") {
			//region -> region to update
			//lname -> lobbyname  to update
			//saddrport -> ip:port of server to update

			// get region:lobbyname from packet
			string lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			region = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = region + ":" + lname;

			//now have region, lobbyname, saddr, sport, and saddrport
			/*
			- create empty vector
			- for each SteamID following lobbyname
			- update currentgame[SteamID] with [region:lobbyname]
			- add to vector
			- map vector to playerlist[region:lobbyname]
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
		// CASE: player get list of servers (ID is SteamID for 'logging in')
		// USE:  pslis ID
		else if (com == "pslis") {
			string uname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			uname = tmp;

			// bad request, no SteamID given
			if (arg == "") {
				tosend = "pserr";
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			} 
			// valid request
			else {
				// remember IP:port for SteamID
				playeraddrs[uname] = saddrport;
				tosend = "psack ";
				//build output list of servers
				for (slit = serverlist.begin(); slit != serverlist.end(); ++slit) {
					tosend += slit->first;
					if (++slit != serverlist.end())
						tosend += ":";
					slit--;
				}

				// send back list of servers
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
		}
		// CASE: player get list of lobbies open for region
		// USE:  pllis region
		else if (com == "pllis") {
			//region -> region to register this ip:port for
			//saddrport -> ip:port of server that has requested to be registered

			// get region from packet
			stringstream ss(arg);
			getline(ss, tmp, ':');
			region = tmp;

			// bad request (no region, region doesn't exist)
			if (region == "" || serverlist.count(region) == 0) {
				cout << "BAD REQUEST no region or bad region\n";
			}
			// valid request
			else {
				tosend = "plack ";
				vector<string> v = serverlist[region];

				//build output list of servers
				for (int i = 0; i < v.size(); ++i) {
					tosend += v[i];
					if (i + 1 != v.size())
						tosend += ":";
				}

				//send plack region:lobby1:lobby2 back to the client
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
		}
		// CASE: player join lobby (sent by player)
		// USE:  pjoin ID:region:lobby
		else if (com == "pjoin") {
			//uname -> username of who is joining this server
			//region -> region that lobby is hosted on
			//lname -> lobbyname of lobby
			//saddrport -> ip:port of user that made this request
			string uname, lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			uname = tmp;
			getline(ss, tmp, ':');
			region = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = region + ":" + lname;

			// bad request (no/bad ID, no/bad region, no/bad lobby)
			if (uname == "" || region == "" || lname == "" ||
				playeraddrs.count(uname) == 0 || serverlist.count(region) == 0 || playerlist.count(slname) == 0){
				cout << "BAD REQUEST no/bad user/region/lobby\n";
			}
			// redundant request (currentgame for player is already region:lobby)
			else if (currentgame.count(uname) > 0 && currentgame[uname] == slname) {
				cout << "Already in lobby" << endl;
				// send ACK back
				saddrport = lobbyport[slname];
				tosend = "pjack " + uname + ":" + lobbyport[slname];
				// send pjack SteamID:IP:port to player
				cout << "sending " << tosend << " to " << p.fromaddr << ":" << p.fromport << endl;
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
			// valid request
			else {
				/*
				- add ip:port for user to playeraddrs
				- get ip:port of region:lobbyname
				1) add user to playerlist[region:lobbyname]
				2) add region:lobbyname to currentgame[SteamID]
				3) send user [ip:port]
				*/

				// build ACK
				playeraddrs[uname] = saddrport;
				saddrport = lobbyport[slname];
				tosend = "pjoin " + uname + ":" + slname;
				string serveraddr = saddrport.substr(0, saddrport.find(":"));
				string serverport = saddrport.substr(saddrport.find(":") + 1, saddrport.length() - saddrport.find(":"));
				si_other.sin_port = htons(atoi(serverport.c_str()));
				si_other.sin_addr.S_un.S_addr = inet_addr(serveraddr.c_str());
				// save unACKed packet
				p.toaddr = serverport;
				p.toport = serveraddr;
				unackedPackets.push_back(p);
				// send pjoin SteamID:region:lobby to server
				cout << "sending " << tosend << " to " << serveraddr << ":" << serverport << endl;
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
		}
		// CASE: receive pjoin ACK from server
		// USE:  pjack ID:region:lobby
		else if (com == "pjack") {
			//uname -> username of who is joining this server
			//region -> region that lobby is hosted on
			//lname -> lobbyname of lobby
			//saddrport -> ip:port of user that made this request
			string uname, lname, slname;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			uname = tmp;
			getline(ss, tmp, ':');
			region = tmp;
			getline(ss, tmp, ':');
			lname = tmp;
			slname = region + ":" + lname;

			// bad request (no/bad ID, no/bad region, no/bad lobby)
			if (uname == "" || region == "" || lname == "" ||
				playeraddrs.count(uname) == 0 || serverlist.count(region) == 0 || playerlist.count(slname) == 0) {
				cout << "BAD REQUEST no/bad user/region/lobby\n";
			}
			// valid request
			else {

				string playeraddr;
				string playerport;
				stringstream s2(playeraddrs[uname]);
				getline(s2, tmp, ':');
				playeraddr = tmp;
				getline(s2, tmp, ':');
				playerport = tmp;

				// if 'pjoin ID:region:lobby is unACKed and isn't already added, add player to lobby and remove unACKed
				// if player is already in that server, do nothing here and just send ACK to player later
				if (currentgame.count(uname) > 0 && currentgame[uname] == slname) {
				}
				// if there is an unACKed packet for this add player to lobby and remove unACKed packet, send ACK to player
				else {
					// check if there is an unACKed packet for this (pjoin ID:region:lobby)
					for (int i = 0; i < unackedPackets.size(); ++i) {
						// check if unACKed packet matches
						if (unackedPackets[i].command == com && unackedPackets[i].arguments == arg) {
							cout << "found an unACKed\n";
							// save data, remove unACKed
							playerlist[slname].push_back(uname);
							currentgame[uname] = slname;
							unackedPackets.erase(unackedPackets.begin() + i);
						}
					}
				}

				tosend = "pjack " + uname + ":" + lobbyport[slname];
				cout << "sending " << tosend << " to " << playeraddr << ":" << playerport << endl;
				si_other.sin_port = htons(atoi(playerport.c_str()));
				si_other.sin_addr.S_un.S_addr = inet_addr(playeraddr.c_str());
				// send pjack SteamID:IP:port to specified user
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
		}
		// CASE: player quit lobby (masterserver removes specified player from playerlist of server theyre connected to)
		// USE:  pquit ID
		else if (com == "pquit") {
			//uname -> steamID of who has quit their lobby
			//saddrport -> ip:port of server player is removed from

			// get username from packet
			string uname, tmp;
			stringstream ss(arg);
			getline(ss, tmp, ':');
			uname = tmp;

			// bad request (no region)
			if (uname == "") {
				cout << "BAD REQUEST\n";
				continue;
			}

			/*
			1) get region:lobbyname from currentgame[uname]
			2) remove uname from playerlist[region:lobbyname]
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
		// CASE: player invites another player to their current game
		// USE: pinvi fromID:toID
		else if (com == "pinvi") {
			string fromname, toname, slname, uaddr, uport, uaddrport, str;
			fromname = arg.substr(0, arg.find(":"));
			toname = arg.substr(arg.find(":") + 1, arg.length() - arg.find(":"));

			string playeraddr = p.fromaddr;
			string playerport = p.fromport;
			
			// bad request (no/bad SteamIDs)
			if (fromname == "" || toname == "" || fromname == toname ||
				playeraddrs.count(fromname) == 0 || playeraddrs.count(toname) == 0) {
				cout << "BAD REQUEST no/bad SteamIDs" << endl;
			}
			// redundant request (in same game already)
			else if (currentgame.count(toname) > 0 && currentgame.count(fromname) > 0 &&
					 currentgame[toname] == currentgame[fromname]) {
				// send ACK BACK TO INVITER
				tosend = "piack " + fromname + ":" + toname;
				cout << "sending " << tosend << " to " << p.fromaddr << ":" << p.fromport << endl;
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
			// valid request
			else {
				//player == inviter
				//u      == invited
				//slname == region:lobby of game

				// find specified players current game (region:lobbyname) and ip:port of who to send it to
				slname = currentgame[fromname];
				uaddrport = playeraddrs[toname];
				uaddr = uaddrport.substr(0, uaddrport.find(":"));
				uport = uaddrport.substr(uaddrport.find(":") + 1, arg.length() - arg.find(":"));

				// send that to the remembered IP:port of INVITED player
				tosend = "pinvi " + fromname + ":" + toname + ":" + slname;
				cout << "sending " << tosend << " to " << uaddr << ":" << uport << endl;
				si_other.sin_port = htons(atoi(uport.c_str()));
				si_other.sin_addr.S_un.S_addr = inet_addr(uaddr.c_str());
				int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
				if (n < 0) perror("sendto");
			}
		}
		// CASE: player invites another player ACK
		// USE: piack fromID:toID
		else if (com == "piack") {
			string fromname, toname, slname, uaddr, uport, uaddrport, str;
			fromname = arg.substr(0, arg.find(":"));
			toname = arg.substr(arg.find(":") + 1, arg.length() - arg.find(":"));

			// bad request (no/bad SteamIDs)
			if (fromname == "" || toname == "" || fromname == toname ||
				playeraddrs.count(fromname) == 0 || playeraddrs.count(toname) == 0) {
				cout << "BAD REQUEST no/bad SteamIDs" << endl;
			}

			// get IP:port of inviter to send ACK back to
			uaddrport = playeraddrs[fromname];
			uaddr = uaddrport.substr(0, uaddrport.find(":"));
			uport = uaddrport.substr(uaddrport.find(":") + 1, arg.length() - arg.find(":"));


			// build ACK to send back to inviter
			tosend = "piack " + fromname + ":" + toname;
			cout << "sending " << tosend << " to " << uaddr << ":" << uport << endl;
			si_other.sin_port = htons(atoi(uaddr.c_str()));
			si_other.sin_addr.S_un.S_addr = inet_addr(uport.c_str());
			// send piack SteamID:IP:port to inviter
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			if (n < 0) perror("sendto");
		}
		else if (com == "clear") {
			vector<Packet> unackedPackets;
			map<string, vector<string>>	serverlist;  // [region] -> ([lobbyname],[lobbyname],[lobbyname])
			map<string, vector<string>> openlobby;   // [region] -> ([ip:port],[ip:port],[ip:port]) of servers without games running
			map<string, string>			lobbyport;   // [region:lobbyname] -> [ip:port]
			map<string, vector<string>> lobbyinfo;   // [region:lobbyname] -> ([maxplayers,currentplayers,etc])
			map<string, vector<string>> playerlist;  // [region:lobbyname] -> [([SteamID, SteamID, SteamID])]
			map<string, string>			currentgame; // [SteamID] -> [region:lobbyname]
			map<string, string>			playeraddrs; // [SteamID] -> [ip:port]
			printf("Master server restarted...\n");
			continue;
		}
		else {
			cout << "!! - INCORRECT INPUT - !!" << endl;
			cout << "!! com = " << com << "!!" << endl;
			cout << "!! arg = " << arg << "!!" << endl;
		}

		//
		// RETRANSMIT UNACKED PACKETS THAT HAVE TIMED OUT
		//
		retransmitunACKed();
		printunACKed();


		printMaps();
		printf("-------------\n");
	}

	closeMaps();
	closesocket(s);
	WSACleanup();

	return 0;
}


//unACKed packets can be
// stlob region:lobby		-- resend "stlob region:lobby" to toaddr,toport
// pjoin ID:region:lobby	-- resend "pjoin ID:region:lobby" to lobbyport[region:lobby]
void retransmitunACKed() {
	for (int i = 0; i < unackedPackets.size(); i++) {
		// if we've retransmitted a bunch already, forget about it
		if (chrono::system_clock::now() - unackedPackets[i].timestamp > (3*RTO)) {
			unackedPackets.erase(unackedPackets.begin() + i);
		}
		// if we haven't retransmitted too much, retransmit
		else if (chrono::system_clock::now() - unackedPackets[i].timestamp > RTO) {
			string tosend, fromname, toname, addrport, addr, port, com, arg, tmp;
			
			Packet p = unackedPackets[i];
			stringstream ss(p.arguments);

			// resend packet to toaddr:toport
			tosend = p.command + " " + p.arguments;
			si_other.sin_port = htons(atoi(p.toport.c_str()));
			si_other.sin_addr.S_un.S_addr = inet_addr(p.toaddr.c_str());
			cout << "Retransmit " << p.command << " " << p.arguments << endl;
			int n = sendto(s, tosend.c_str(), tosend.length(), 0, (sockaddr*)&si_other, slen);
			// if there's an error resending, dont update timestamp (drop packets eventually)
			if (n < 0) {
				perror("Retransmission");
			}
			// if there's no error resending, update timestamp so it keeps retransmitting
			else {
				unackedPackets[i].timestamp = chrono::system_clock::now();
			}
		}
	}
}

void printunACKed() {
	for (int i = 0; i < unackedPackets.size(); ++i) {
		cout << "- unacked" << i << " contains: " << unackedPackets[i].command 
			<< " " << unackedPackets[i].arguments << endl;
	}
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
