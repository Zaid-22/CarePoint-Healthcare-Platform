import { configureStore } from '@reduxjs/toolkit';
import authReducer from './slices/authSlice';
import { logout } from './slices/authSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
  },
});

window.addEventListener('carepoint:auth-cleared', () => {
  store.dispatch(logout());
});

window.addEventListener('storage', (event) => {
  if (['accessToken', 'refreshToken', 'user'].includes(event.key ?? '') &&
      !localStorage.getItem('accessToken')) {
    store.dispatch(logout());
  }
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
