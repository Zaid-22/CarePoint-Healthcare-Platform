import { configureStore } from '@reduxjs/toolkit';
import authReducer, { logout } from './slices/authSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
  },
});

window.addEventListener('carepoint:auth-cleared', () => {
  store.dispatch(logout());
});

// Remove credentials persisted by older CarePoint frontend versions.
localStorage.removeItem('accessToken');
localStorage.removeItem('refreshToken');
localStorage.removeItem('user');

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
