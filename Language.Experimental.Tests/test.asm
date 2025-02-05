format PE console
entry Main
section '.data' data readable writeable
	!str_0 db 0x48,0x65,0x6C,0x6C,0x6F,0x20,0x77,0x6F,0x72,0x6C,0x64,0x21,0 ; "Hello world!"
section '.text' code readable executable
		Main:
			_Main@0:
			push ebp
			mov ebp, esp
			sub esp, 4
			push !str_0
			call indirect_print
			call eax
			add esp, 4
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
