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
#include<stdio.h>
#include <iostream>

#pragma comment(lib,"ws2_32.lib") //Winsock Library

#define BUFLEN 512  //Max length of buffer

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
	/*
	char saddr[32];
	char sport[32];
	printf("Enter IP  of masterserver : ");
	gets_s(saddr);
	printf("Enter port of masterserver : ");
	gets_s(sport);
	*/

	char saddr[] = "127.0.0.1";
	char sport[] = "8484";

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
		std::string com = (std::string)message; com = com.substr(0, 5);

		//send the message to masterserver
		if (com != "recv") {
			if (sendto(s, message, strlen(message), 0, (struct sockaddr *) &si_other, slen) == SOCKET_ERROR)
			{
				printf("sendto() failed with error code : %d", WSAGetLastError());
				exit(EXIT_FAILURE);
			}
		}

		//receive a reply and print it
		memset(buf, '\0', BUFLEN);
		if (com  == "recv"  || com == "stser" || com == "stlob" ||
			    com == "stser" || com == "close" || com == "pslis" ||
			    com == "pllis" || com == "pquit"){
			if (recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen) == SOCKET_ERROR)
			{
				printf("recvfrom() failed with error code : %d", WSAGetLastError());
				exit(EXIT_FAILURE);
			}

			while (buf == "") {
				recvfrom(s, buf, BUFLEN, 0, (struct sockaddr *) &si_other, &slen);
			}
			printf("%s|\n", buf);
		}
	}

	closesocket(s);
	WSACleanup();

	return 0;
}
