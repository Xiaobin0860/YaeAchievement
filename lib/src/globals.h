#pragma once
#include <Windows.h>
#include <unordered_set>

#define PROPERTY2(type, name, cn, os) \
	type name##_cn = cn; \
	type name##_os = os; \
	type get_##name() { return Globals::IsCNREL ? name##_cn : name##_os; } \
    void set_##name(type value) { if (Globals::IsCNREL) name##_cn = value; else name##_os = value; } \
	__declspec(property(get = get_##name, put = set_##name)) type name;

namespace Globals
{
	inline HWND GameWindow = nullptr;
	inline HANDLE MessagePipe = nullptr;
	inline bool IsCNREL = true;
	inline uintptr_t BaseAddress = 0;

	// 5.1.0 - 24082
	inline uint16_t CmdId = 0; // use non-zero to override dynamic search
	inline std::unordered_set<uint16_t> DynamicCmdIds;

	class Offsets
	{
	public:
		PROPERTY2(uintptr_t, BitConverter_ToUInt16, 0, 0);
		//PROPERTY2(uintptr_t, BitConverter_ToUInt16, 0x0F826CF0, 0x0F825F10); // use non-zero to override dynamic search
	};

	inline Offsets Offset;
}