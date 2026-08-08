import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import api, { requestSessionRefresh, setAccessToken } from '../../api/client';
import type { AuthUser, AuthResponse, LoginRequest, RegisterRequest, ApiResponse } from '../../types';

interface AuthState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  initialized: boolean;
  loading: boolean;
  error: string | null;
}

export const initializeSession = createAsyncThunk<
  AuthResponse,
  void,
  { rejectValue: string; state: { auth: AuthState } }
>(
  'auth/initializeSession',
  async (_, { rejectWithValue }) => {
    try {
      return await requestSessionRefresh();
    } catch {
      setAccessToken(null);
      return rejectWithValue('No active session');
    }
  },
  {
    condition: (_, { getState }) => {
      const auth = getState().auth;
      return !auth.initialized && !auth.loading;
    },
  },
);

export const login = createAsyncThunk<AuthResponse, LoginRequest, { rejectValue: string }>(
  'auth/login',
  async (credentials, { rejectWithValue }) => {
    try {
      const { data } = await api.post<ApiResponse<AuthResponse>>('/auth/login', credentials);
      setAccessToken(data.data.accessToken);
      return data.data;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message || 'Login failed');
    }
  },
);

export const register = createAsyncThunk<AuthResponse, RegisterRequest, { rejectValue: string }>(
  'auth/register',
  async (payload, { rejectWithValue }) => {
    try {
      const { data } = await api.post<ApiResponse<AuthResponse>>('/auth/register', payload);
      setAccessToken(data.data.accessToken);
      return data.data;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message || 'Registration failed');
    }
  },
);

export const logoutFromServer = createAsyncThunk<void, void>(
  'auth/logoutFromServer',
  async (_, { dispatch }) => {
    try {
      await api.post('/auth/logout');
    } finally {
      setAccessToken(null);
      dispatch(logout());
    }
  },
);

const extractUser = (payload: AuthResponse): AuthUser => {
  const roles = payload.roles && payload.roles.length > 0
    ? payload.roles
    : payload.role
      ? [payload.role]
      : [];

  return {
    userId: payload.userId,
    email: payload.email,
    firstName: payload.firstName,
    lastName: payload.lastName,
    role: payload.role,
    roles,
  };
};

const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  initialized: false,
  loading: false,
  error: null,
};

const applySession = (state: AuthState, payload: AuthResponse) => {
  state.user = extractUser(payload);
  state.isAuthenticated = true;
  state.initialized = true;
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    logout(state) {
      state.user = null;
      state.isAuthenticated = false;
      state.initialized = true;
      state.error = null;
    },
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(initializeSession.pending, (state) => {
        state.loading = true;
      })
      .addCase(initializeSession.fulfilled, (state, { payload }) => {
        state.loading = false;
        applySession(state, payload);
      })
      .addCase(initializeSession.rejected, (state) => {
        state.loading = false;
        state.user = null;
        state.isAuthenticated = false;
        state.initialized = true;
      })
      .addCase(login.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, { payload }) => {
        state.loading = false;
        applySession(state, payload);
      })
      .addCase(login.rejected, (state, { payload }) => {
        state.loading = false;
        state.error = payload ?? 'Login failed';
      })
      .addCase(register.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(register.fulfilled, (state, { payload }) => {
        state.loading = false;
        applySession(state, payload);
      })
      .addCase(register.rejected, (state, { payload }) => {
        state.loading = false;
        state.error = payload ?? 'Registration failed';
      });
  },
});

export const { logout, clearError } = authSlice.actions;
export default authSlice.reducer;
