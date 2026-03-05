import {format} from 'date-fns'

export function calculateDaysBetweenDates(startDate, endDate) {
    // Parse the dates
    const start = new Date(startDate);
    const end = new Date(endDate);
    
    // Calculate the difference in time
    const differenceInTime = end - start;
    
    // Convert the time difference to days
    const differenceInDays = differenceInTime / (1000 * 3600 * 24);
    
    return differenceInDays + 1;
  }

  export function formatDate(date) {
    
    const formatedDate = format(new Date(date), 'yyyy-MM-dd')
    
    return formatedDate;
  }

  export function formatDateWithoutTimezone(date) {
    
    const formatedDate = format(new Date(date), 'yyyy-MM-dd HH:mm:ss')
    
    return formatedDate;
  }

  export function formatDateMonth(date){
    const formatedDate = format(new Date(date), 'MM/dd/yyyy')
    
    return formatedDate;
  }
