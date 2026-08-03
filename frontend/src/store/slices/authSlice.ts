import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import api from '../../api/client';
import type { AuthUser, AuthResponse, LoginRequest, RegisterRequest, ApiResponse } from '../../types';

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
}

export const login = createAsyncThunk<AuthResponse, LoginRequest, { rejectValue: string }>(
  'auth/login',
  async (credentials, { rejectWithValue }) => {
    try {
      const { data } = await api.post<ApiResponse<AuthResponse>>('/auth/login', credentials);
      return data.data;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message || 'Login failed');
    }
  }
);

export const register = createAsyncThunk<AuthResponse, RegisterRequest, { rejectValue: string }>(
  'auth/register',
  async (payload, { rejectWithValue }) => {
    try {
      const { data } = await api.post<ApiResponse<AuthResponse>>('/auth/register', payload);
      return data.data;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message || 'Registration failed');
    }
  }
);

export const logoutFromServer = createAsyncThunk<void, void>(
  'auth/logoutFromServer',
  async (_, { dispatch }) => {
    const refreshToken = localStorage.getItem('refreshToken');
    try {
      if (refreshToken) {
        await api.post('/auth/logout', { refreshToken });
      }
    } finally {
      dispatch(logout());
    }
  }
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

const getSavedUser = (): AuthUser | null => {
  const savedUser = localStorage.getItem('user');
  if (!savedUser) return null;
  try {
    const parsed = JSON.parse(savedUser);
    return {
      ...parsed,
      roles: parsed.roles && Array.isArray(parsed.roles) ? parsed.roles : (parsed.role ? [parsed.role] : []),
    };
  } catch {
    return null;
  }
};

const initialState: AuthState = {
  user: getSavedUser(),
  accessToken: localStorage.getItem('accessToken'),
  isAuthenticated: !!localStorage.getItem('accessToken'),
  loading: false,
  error: null,
};

const persistAuth = (payload: AuthResponse) => {
  const user = extractUser(payload);
  localStorage.setItem('accessToken', payload.accessToken);
  localStorage.setItem('refreshToken', payload.refreshToken);
  localStorage.setItem('user', JSON.stringify(user));
};

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    logout(state) {
      state.user = null;
      state.accessToken = null;
      state.isAuthenticated = false;
      state.error = null;
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
    },
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, { payload }) => {
        state.loading = false;
        state.user = extractUser(payload);
        state.accessToken = payload.accessToken;
        state.isAuthenticated = true;
        persistAuth(payload);
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
        state.user = extractUser(payload);
        state.accessToken = payload.accessToken;
        state.isAuthenticated = true;
        persistAuth(payload);
      })
      .addCase(register.rejected, (state, { payload }) => {
        state.loading = false;
        state.error = payload ?? 'Registration failed';
      });
  },
});

export const { logout, clearError } = authSlice.actions;
export default authSlice.reducer;
