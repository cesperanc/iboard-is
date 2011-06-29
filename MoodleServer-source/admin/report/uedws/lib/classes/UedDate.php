<?php
	/**
	 * Date object to store a date in a more readable format
	 * 
	 * @author Cláudio Esperança, Diogo Serra
	 */
	class UedDate{
		/** @var string hour */
		public $hour;
		/** @var string minute */
		public $minute;
		/** @var string second */
		public $second;
		/** @var string day */
		public $day;
		/** @var string month */
		public $month;
		/** @var string year */
		public $year;
		
		/**
		 * Constructor
		 * @param string $unixtimestamp with the unix time stamp to use
		 */
		public function __construct($unixtimestamp=null){
			date_default_timezone_set('UTC');
			if(!empty($unixtimestamp)){
				$this->year = date("Y", $unixtimestamp);
				$this->month = date("m", $unixtimestamp);
				$this->day = date("d", $unixtimestamp);
				$this->hour = date("G", $unixtimestamp);
				$this->minute = date("i", $unixtimestamp);
				$this->second = date("s", $unixtimestamp);
			}
		}
		
		/**
	     * The unix timestamp
	     * 
	     * @return string with the unix timestamp or boolean false
	     */
		public function toUnixTimestamp(){
			date_default_timezone_set('UTC');
			
			if($timestamp = mktime($this->hour, $this->minute, $this->second, $this->month, $this->day, $this->year)){
				return $timestamp;
			}
			return false;
		}
	}
?>
