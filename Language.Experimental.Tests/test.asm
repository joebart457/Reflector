format PE console
entry Main
section '.data' data readable writeable
	!str_0 db 0x48,0x65,0x6C,0x6C,0x6F,0x2C,0x20,0x6E,0x65,0x77,0x20,0x77,0x6F,0x72,0x6C,0x64,0x21,0 ; "Hello, new world!"
section '.text' code readable executable
		Main:
			_Main@0:
			push ebp
			mov ebp, esp
			sub esp, 4
			call GetString_Getter
			push eax
			call printerFactory!string
			mov esp, ebp
			pop ebp
			ret
		indirect_print:
			_indirect_print@0:
			push ebp
			mov ebp, esp
			sub esp, 4
			push dword [printf]
			pop eax
			mov esp, ebp
			pop ebp
			ret
		GetString_Getter:
			_GetString_Getter@0:
			push ebp
			mov ebp, esp
			sub esp, 4
			push GetString
			pop eax
			mov esp, ebp
			pop ebp
			ret
		GetString:
			_GetString@0:
			push ebp
			mov ebp, esp
			sub esp, 4
			push !str_0
			pop eax
			mov esp, ebp
			pop ebp
			ret
		printerFactory!string:
			_printerFactory!string@4:
			push ebp
			mov ebp, esp
			sub esp, 4
			call dword [ebp + 8]
			push eax
			call dword [printf]
			add esp, 4
			mov esp, ebp
			pop ebp
			ret 4
section '.idata' import data readable writeable
	dd !lib_0_ilt,0,0,RVA !lib_0_name, RVA !lib_0_iat
	dd 0,0,0,0,0
	!lib_0_name db 'msvcrt.dll',0
	rb RVA $ and 1
	rb(-rva $) and 3
	!lib_0_ilt:
	dd RVA !printf
	dd 0
	!lib_0_iat:
	printf dd RVA !printf
	dd 0
	!printf dw 0
	db 'printf',0
