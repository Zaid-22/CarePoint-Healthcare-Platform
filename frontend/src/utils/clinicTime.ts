const clinicTimeZone = import.meta.env.VITE_CLINIC_TIME_ZONE || 'Asia/Amman';

export const getClinicDateString = (date = new Date()): string => {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: clinicTimeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).formatToParts(date);
  const value = (type: Intl.DateTimeFormatPartTypes) =>
    parts.find((part) => part.type === type)?.value ?? '';

  return `${value('year')}-${value('month')}-${value('day')}`;
};
