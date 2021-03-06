// masterclient.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#define _WINSOCK_DEPRECATED_NO_WARNINGS
/*
Simple udp client
*/
#include<stdio.h>
#include<winsock2.h>
#include<string>
#include<sstream>
#include<stdio.h>
#include <iostream>

#pragma comment(lib,"ws2_32.lib") //Winsock Library

#define BUFLEN 512  //Max length of buffer

using namespace std;

enum PacketType : int {
	CREATE_LOBBY,
	REFRESH_LOBBY_LIST,
	JOIN_LOBBY,
	LEAVE_LOBBY,
	START_GAME,
	SNAPSHOT,
	BULLET_SNAPSHOT,
	PLAYER_SNAPSHOT,
	PLAYER_INPUT,
	INVITE_PLAYER
};

string region;
string lobby;

int main(void)
{
	struct sockaddr_in si_other;
	int s, slen = sizeof(si_other);
	char buf[BUFLEN];
	char message[BUFLEN];
	WSADATA wsa;

	//Initialise winsock
	//printf("\nInitialising Winsock...");
	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
	{
		printf("Failed. Error Code : %d", WSAGetLastError());
		exit(EXIT_FAILURE);
	}
	//printf("Initialised.\n");

	//create socket
	if ((s = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP)) == SOCKET_ERROR)
	{
		printf("socket() failed with error code : %d", WSAGetLastError());
		exit(EXIT_FAILURE);
	}

	//get ip:port of masterserver
	
	char saddr[32];
	//char sport[32];
	printf("Enter IP  of masterserver : ");
	gets_s(saddr);
	//printf("Enter port of masterserver : ");
	//gets_s(sport);
	

	//char saddr[] = "127.0.0.1";
	char sport[] = "8484";

	//set socket to non-blocking
	u_long mode = 1;
	int nonblock = ioctlsocket(s, FIONBIO, &mode);
	if (nonblock != NO_ERROR)
		printf("ioctlsocket failed with error: %ld\n", nonblock);

	//setup address structure
	memset((char *)&si_other, 0, sizeof(si_other));
	si_other.sin_family = AF_INET;
	si_other.sin_port = htons(atoi(sport));
	si_other.sin_addr.S_un.S_addr = inet_addr(saddr);

	printf("Connecting to %s:%i\n", inet_ntoa((&si_other)->sin_addr), ntohs(si_other.sin_port));


	//
	// END OF BOILERPLATE
	//

	char b[BUFLEN];
	//start communication
	while (1)
	{
		// Get message from stdin
		printf("Enter message : ");
		gets_s(message);
		string com = (string)message; com = com.substr(0, 5);

		//send the message to masterserver if not "recv" or "wait"
		if (com != "recv" && com != "wait") {
			if (sendto(s, message, strlen(message), 0, (struct sockaddr *) &si_other, slen) == SOCKET_ERROR)
			{
				printf("sendto() failed with error code : %d", WSAGetLastError());
				exit(EXIT_FAILURE);
			}
		}

		//receive a reply and print it for these packets
		memset(buf, '\0', BUFLEN);
		if (com == "recv" || com == "stser" || com == "stlob" ||
			com == "stser" || com == "close" || com == "pslis" ||
			com == "pllis" || com == "pquit") {
			if (recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen) == SOCKET_ERROR)
			{
				int err = WSAGetLastError();
				if (err != WSAEWOULDBLOCK) {
					printf("recvfrom() failed with error code : %d", err);
					exit(EXIT_FAILURE);
				}
			}

			while (recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen) > 0) {
				printf("%s|\n", buf);
			}
			printf("%s|\n", buf);
		}
		// this is a player waiting to be invited 
		// listen for pinvi fromID:toID:region:lobby 
		// send piack fromID:toID
		if (com == "wait") {
			if (recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen) == SOCKET_ERROR)
			{
				int err = WSAGetLastError();
				if (err != WSAEWOULDBLOCK) {
					printf("recvfrom() failed with error code : %d", err);
					exit(EXIT_FAILURE);
				}
			}

			string com = (string)buf; com = com.substr(0, 5);
			string arg = (string)(buf + 6);
			string tmp;
			stringstream ss(arg);
			getline(ss, tmp, ':'); // fromID
			string fromID = tmp;
			getline(ss, tmp, ':'); // toID
			string toID = tmp;


			string str = "piack " + fromID + ":" + toID;
			if (sendto(s, str.c_str(), str.length(), 0, (struct sockaddr *) &si_other, slen) == SOCKET_ERROR)
			{
				printf("sendto() failed with error code : %d", WSAGetLastError());
				exit(EXIT_FAILURE);
			}
		}
		// this is a player inviting 
		// listen for piack fromID:toID
		if (com == "pinvi") {
			if (recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen) == SOCKET_ERROR)
			{
				int err = WSAGetLastError();
				if (err != WSAEWOULDBLOCK) {
					printf("recvfrom() failed with error code : %d", err);
					exit(EXIT_FAILURE);
				}
			}
			printf("%s|\n", buf);
		}
		// this is a player joining a server 
		// save region:lobby from sent pjoin ID:region:lobby
		// listen for pjack ID:IP:port
		if (com == "pjoin") {

		}
		// this is a server 
		// listen for pjoin ID:region:lobby
		// send back pjack ID:region:lobby
		if (com == "stser") {

		}
	}

	closesocket(s);
	WSACleanup();

	return 0;
}
