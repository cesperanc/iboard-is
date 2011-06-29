<?php
/**
 * Main web service class file for the module
 * 
 * @package		UEDWS
 * @subpackage	report_uedws
 * @version		2010.0
 * @copyright 	Learning Distance Unit {@link http://ued.ipleiria.pt}, Polytechnic Institute of Leiria
 * @author		Cláudio Esperança <cesperanc@gmail.com>
 * @license 	GNU GPL v3 or later {@link http://www.gnu.org/copyleft/gpl.html}
 */
class UEDWS {
	/** @var string AUTHENTICATIONERROR */
	public static $AUTHENTICATIONERROR = '-1';
	
	/** @var string UEDWSAUTHENTICATIONPLUGINDISABLED */
	public static $UEDWSAUTHENTICATIONPLUGINDISABLED = '-2';
	
	/** @var string INVALIDTIMESTAMPERROR */
	public static $INVALIDTIMESTAMPERROR = '-3';
	
	/** @var string UEDDATECONVERSIONFAILED */
	public static $UEDDATECONVERSIONFAILED = '-3';
	
	/** @var string WITHOUTACCESSTOCOURSE */
	public static $WITHOUTACCESSTOCOURSE = '-5';
	
	/** @var string COURSENOTFOUND */
	public static $COURSENOTFOUND = '-6';
	
	/** @var string USERNOTFOUND */
	public static $USERNOTFOUND = '-7';
	
	/** @var string INSUFFICIENTPERMISSIONS */
	public static $INSUFFICIENTPERMISSIONS = '-8';
	
	// shhh... private class things here
	private static $LOCALENAME = 'report_uedws';
	private static $HASCOURSEACCESS = array();
	
	/**
	 * Verify if the user is authenticated (optionally, if the supplied username is the same of the authenticated user)
	 * 
	 * @param string $username, with the username to check for
	 * @return boolean true on success, false otherwise
	 * @uses global $USER
	 */
	private static function _isAuthenticated($username=NULL){
		global $USER;
		if(isloggedin()){
			if(!is_null($username) && isset($USER->username) && $USER->username !== $username){
				return false;
			}
			return true;
		}
		return false;
	}
	
	/**
	 * Verify if everything is ok to use the webservice
	 * 
	 * @param UedCredentials $credentials with the username and autorization key
	 * @return boolean true on success, false otherwise or throw a soap fault on error
	 */
	private static function _validate($credentials){
		if(!is_enabled_auth('uedws')){
    		throw new SoapFault(self::$UEDWSAUTHENTICATIONPLUGINDISABLED, get_string('uedws_theuedwsauthenticationpluginisdisabled', self::$LOCALENAME));
    	}
		return (self::_isAuthenticated() || self::validateCredentials($credentials));
	}
	
	/**
     * Execute the common verification for the getCourseRecent* methods and return the timestamp to use on the specific processing
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @param int $courseId with the courseId to get recent activity from
     * @param UedDate $UedStartDate with the start date to get events from
     * @return string with timestamp or throw a SoupFault on error
     */
    private static function _getCourseRecent($credentials, $courseId, $UedStartDate=NULL){
		global $CFG, $USER;
		
		// check credentials
		if(!self::_validate($credentials)):
			throw new SoapFault(self::$AUTHENTICATIONERROR, get_string('uedws_authenticationerror', self::$LOCALENAME));
		endif;
		
		// check for course access
		if(!self::canAccessToCourse($credentials, $courseId)):
			throw new SoapFault(self::$WITHOUTACCESSTOCOURSE, get_string('uedws_withoutaccesstocourse', self::$LOCALENAME));
		endif;
		
		$context = get_context_instance(CONTEXT_COURSE, $courseId);
		
		// start date
		$timestart = round(time() - COURSE_MAX_RECENT_PERIOD, -2);
		if(!has_capability('moodle/legacy:guest', $context, NULL, false)){
			if(!empty($USER->lastcourseaccess[$courseId])){
				if($USER->lastcourseaccess[$courseId] > $timestart){
					$timestart = $USER->lastcourseaccess[$courseId];
				}
			}
		}
		if(!empty($UedStartDate) && $timestamp = self::_uedDateToUnixTimestamp($credentials, $UedStartDate)){
			$timestart = $timestamp;
		}
		
	    return $timestart;
    }
    
	/**
     * Return the recent module activity within a course
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @param int $courseId with the courseId to get recent activity from
     * @param UedDate $UedStartDate with the start date to get events from
     * @param string $moduleName with the module name to get events from
     * @return object[] with all the recent activities or throw a SoupFault on error
     */
    private static function _getCourseRecentActivity($credentials, $courseId, $UedStartDate=NULL, $moduleName=NULL){
		global $CFG, $USER;
		
		if(empty($moduleName)){
			return array();
		}
		
		$timestart = self::_getCourseRecent($credentials, $courseId, $UedStartDate);
		
		$context = get_context_instance(CONTEXT_COURSE, $courseId);
		$viewfullnames = has_capability('moodle/site:viewfullnames', $context);
		
		// load the course data
    	if (!$course = get_record('course', 'id', $courseId)) {
    		throw new SoapFault(self::$COURSENOTFOUND, get_string('uedws_coursenotfound', self::$LOCALENAME));
		}
		
		$modinfo =& get_fast_modinfo($course);
		
		$usedmodules = array();
		foreach($modinfo->cms as $cm){
			if($cm->modname!=$moduleName){
				continue;
			}
			if(isset($usedmodules[$cm->modname])){
				continue;
			}
			if(!$cm->uservisible){
				continue;
			}
			$usedmodules[$cm->id] = $cm->modname;
		}
		
		
		$index=0;
		$activities = array();
		
		foreach($usedmodules as $cmid=>$modname){
			if(is_readable($CFG->dirroot.'/mod/'.$modname.'/lib.php')){
				include_once($CFG->dirroot.'/mod/'.$modname.'/lib.php');
				$libfunction = $modname."_get_recent_mod_activity";
				if(function_exists($libfunction)){
					$libfunction($activities, $index, $timestart, $courseId, $cmid);
				}
			}
		}
		
	    return $activities;
    }
	
	/**
     * Check is the webservice is enabled
     *
	 * @return boolean true if the webservice is enabled, false otherwise
     */
    public static function isWebserviceEnabled(){
    	return is_enabled_auth('uedws');
    }
	
	/**
     * Request an autorization key to the webservice using the user credentials
     *
     * @param string $username with the user name
     * @param string $password with the password
     * @param string $deviceIdentification with the device identification string
	 * @return string with the autorization key or an empty string
	 * 
	 * @uses global $modulename
     */
    public static function getAuthorizationKey($username, $password, $deviceIdentification=''){
    	global $modulename;
    	
    	if(!is_enabled_auth('uedws')){
    		throw new SoapFault(self::$UEDWSAUTHENTICATIONPLUGINDISABLED, get_string('uedws_theuedwsauthenticationpluginisdisabled', self::$LOCALENAME));
    	}
    	
    	$autorizationKey = '';
		if(!empty($username) && !empty($password) && $user = authenticate_user_login($username, $password)){
			$user = complete_user_login($user);
			
			/*// let's not pollute the log
			if(!empty($modulename) && function_exists('add_to_log')){
				add_to_log(SITEID, $modulename, 'login', "../../../admin/report/uedws/index.php?action=view", get_string('uedws_theauthenticationfortheuserwascompletedwithsuccess', self::$LOCALENAME),0,$user->id);
			}
			*/
			if(!empty($deviceIdentification)){
				$record = get_record('uedws_authentication', 'userid', $user->id, 'deviceidentification', $deviceIdentification);
			}else{
				$records = get_records('uedws_authentication', 'userid', $user->id, '', '*', 0, 1);
				foreach ($records as $record){
					break;
				}
			}
			
			if($record===false || empty($record->autorizationkey)){
				// generate a key for the user
				$tmpAutorizationKey = '';
				if(function_exists('hash')){
					$tmpAutorizationKey = hash('sha256', uniqid($username, true), false);
				}else{
					$tmpAutorizationKey = md5(uniqid($username, true), false);
				}
				$record = new stdClass();
				$record->userid = $user->id;
				$record->username = $username;
				$record->autorizationkey = $tmpAutorizationKey;
				$record->deviceidentification = ($deviceIdentification)?$deviceIdentification:'';
				$record->creationdate = time();
				$record->lastaccess = time();
				$record->accesscount = '0';
				
				if(insert_record('uedws_authentication', $record)){
					$autorizationKey = $tmpAutorizationKey;
				}
				
			}else{
				//update the use count and last access
				$record->accesscount++;
				$record->lastaccess = time();
				if(update_record('uedws_authentication', $record)){
					$autorizationKey = $record->autorizationkey;
				}
			}
		}
		return $autorizationKey;
    }
    
	/**
     * Revoke the autorization key within the credentials object associated to the specified device identifier
     *
     * @param UedCredentials $credentials with the username and autorization key to revoke
     * @param string $deviceIdentification with the device identification string
	 * @return boolean true on success, false otherwise
	 * 
	 * @uses global $USER
     */
    public static function revokeAuthorizationKey($credentials, $deviceIdentification=''){
    	global $USER;
    	
    	if(!self::_validate($credentials)):
			throw new SoapFault(self::$AUTHENTICATIONERROR, get_string('uedws_authenticationerror', self::$LOCALENAME));
		endif;
    	
		if(!empty($deviceIdentification)){
			if(delete_records('uedws_authentication', 'userid', $USER->id, 'deviceidentification', $deviceIdentification)){
				return true;
			}
		}else{
			if(delete_records('uedws_authentication', 'userid', $USER->id)){
				return true;
			}
		}
		return false;
    }
    
	/**
     * Verify if the credentials are valid and can be used by the webservice
     *
     * @param UedCredentials $credentials with the username and autorization key
	 * @return boolean true on success, false otherwise
	 * 
	 * @uses global $modulename, auth_uedws
     */
    public static function validateCredentials($credentials){
    	global $modulename;
    	
    	if(!is_enabled_auth('uedws')){
    		throw new SoapFault(self::$UEDWSAUTHENTICATIONPLUGINDISABLED, get_string('uedws_theuedwsauthenticationpluginisdisabled', self::$LOCALENAME));
    	}
    	
    	$auth = get_auth_plugin('uedws');
    	
		if(!empty($credentials->username) && !empty($credentials->autorizationKey) && $user = $auth->authenticateUedUser($credentials->username, $credentials->autorizationKey)){
			$user = complete_user_login($user);
			
			/*// let's not pollute the log
			if(!empty($modulename)){
				add_to_log(SITEID, $modulename, 'login', "../../../admin/report/uedws/index.php?action=view", get_string('uedws_theauthenticationfortheuserwascompletedwithsuccess', self::$LOCALENAME),0,$user->id);
			}
			*/
			return self::_isAuthenticated();
		}
		return false;
    }
    
    /**
     * Convert an unix timestamp to UedDate
     * 
     * @param UedCredentials $credentials with the username and autorization key
     * @param string $timeStamp with the unix timestamp to convert
     * @return UedDate with the date, or throw a soap fault
     */
	public static function unixTimestampToUedDate($credentials, $timeStamp){
		if(self::_validate($credentials)){
			return new UedDate($timeStamp);
			
			throw new SoapFault(self::$INVALIDTIMESTAMPERROR, get_string('uedws_invalidtimestamperror', self::$LOCALENAME));
		}
		throw new SoapFault(self::$AUTHENTICATIONERROR, get_string('uedws_authenticationerror', self::$LOCALENAME));
	}
    /**
     * Convert an UedDate to an unix timestamp
     * 
     * @param UedCredentials $credentials with the username and autorization key
     * @param UedDate $uedDate with the date to convert from
     * @return string with the unix timestamp or false
     */
	public static function _uedDateToUnixTimestamp($credentials, $uedDate){
		date_default_timezone_set('UTC');
		if(self::_validate($credentials)){
			if(method_exists($uedDate, 'toUnixTimestamp') && $timestamp = $uedDate->toUnixTimestamp()){
				return $timestamp;
			}else{
				if((isset($uedDate->hour) && isset($uedDate->minute) && isset($uedDate->second) && isset($uedDate->month) && isset($uedDate->day) && isset($uedDate->year)) && $timestamp = mktime($uedDate->hour, $uedDate->minute, $uedDate->second, $uedDate->month, $uedDate->day, $uedDate->year)){ // let's try again
					return $timestamp;
				}
			}
		}
		return false;
	}
	
    /**
     * Convert an UedDate to an unix timestamp
     * 
     * @param UedCredentials $credentials with the username and autorization key
     * @param UedDate $uedDate with the date to convert from
     * @return string with the unix timestamp
     */
	public static function uedDateToUnixTimestamp($credentials, $uedDate){
		date_default_timezone_set('UTC');
		if(self::_validate($credentials)){
			if($timestamp = self::_uedDateToUnixTimestamp($credentials, $uedDate)){
				return $timestamp;
			}
			// If the conversion failed, throw an error
			throw new SoapFault(self::$UEDDATECONVERSIONFAILED, get_string('uedws_conversionfailederror', self::$LOCALENAME));
		}
		throw new SoapFault(self::$AUTHENTICATIONERROR, get_string('uedws_authenticationerror', self::$LOCALENAME));
	}
    
    /**
     * Get the courses of the user
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @return UedCourse[] with the authenticated user courses or SoapFault
     */
    public static function getMyCourses($credentials){
		global $CFG, $USER;
		if(self::_validate($credentials)){
    		$userCourses = array();
    		$courses = get_my_courses($USER->id, null, array('id', 'category', 'shortname', 'fullname', 'idnumber', 'startdate', 'visible', 'newsitems'), false, 0);
			foreach($courses as $course){
				$courseObject =  new UedCourse();
				$courseObject->id = $course->id;
				$courseObject->category = $course->category;
				$courseObject->shortName = $course->shortname;
				$courseObject->fullName = $course->fullname;
				$courseObject->idNumber = $course->idnumber;
				$courseObject->startDate = self::unixTimestampToUedDate($credentials, $course->startdate);
				$courseObject->visible = $course->visible;
				$courseObject->newsItems = $course->newsitems;
				$courseObject->fullLink = "{$CFG->wwwroot}/course/view.php?id={$course->id}";
				$courseObject->visible = $course->visible;
				
				$userCourses[] = $courseObject;
			}
        	return $userCourses;
        }
        
        throw new SoapFault(self::$AUTHENTICATIONERROR, get_string('uedws_authenticationerror', self::$LOCALENAME));
    }
    
	/**
     * Verify if the user can access to the information of a specific course
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @param int $courseId with the courseId to check
     * @return boolean true if it can, false if not, SoupFault on other errors
     */
    public static function canAccessToCourse($credentials, $courseId){
		global $CFG, $USER;
		if(self::_validate($credentials)){
			// if we have this course result on cache, return it
			if(isset(self::$HASCOURSEACCESS[$courseId])){
				return self::$HASCOURSEACCESS[$courseId];
			}else{
				// default to no access
				self::$HASCOURSEACCESS[$courseId] = false;
			}
			
			// check if the course exists
			if (!$course = get_record('course', 'id', $courseId)) {
				return false; // we are not lying were! The course doesn't exist, so the user really can't access to it...
			}
			
			// Fetch the course context, and prefetch its child contexts
			if(!isset($course->context)){
				if(!$course->context=get_context_instance(CONTEXT_COURSE, $course->id)){
					return false;
				}
			}
			
			// check if the user can be in a particular course
			if(empty($USER->access['rsw'][$course->context->path])){
				// If the course or the parent category are hidden and the user hasn't the 'course:viewhiddencourses' capability, prevent access
				if(!($course->visible && course_parent_visible($course)) && !has_capability('moodle/course:viewhiddencourses', $course->context)){
					return false;
				}
			}
			
			// non-guests who don't currently have access, check if they can be allowed in as a guest
			if($USER->username!='guest' && !has_capability('moodle/course:view', $course->context)){
				if($course->guest==1){
					// Temporarily assign guest role to the user for this context
					$USER->access = load_temp_role($course->context, $CFG->guestroleid, $USER->access);
				}
			}
			
			// check if the user can view the course
			if(has_capability('moodle/course:view', $course->context)){
				self::$HASCOURSEACCESS[$courseId] = true;
				return true;
			}
			return false;
        }
        
        throw new SoapFault(self::$AUTHENTICATIONERROR, get_string('uedws_authenticationerror', self::$LOCALENAME));
    }
    
	/**
     * Return the recent enrolments in a course
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @param int $courseId with the courseId to get recent activity from
     * @param UedDate $UedStartDate with the start date to get events from
     * @return UedRecentEnrolment[] with all the recent enrolments or throw a SoupFault on error
     */
    public static function getCourseRecentEnrolments($credentials, $courseId, $UedStartDate=NULL){
		global $CFG, $USER;
		
		$timestart = self::_getCourseRecent($credentials, $courseId, $UedStartDate);
		
		$context = get_context_instance(CONTEXT_COURSE, $courseId);
		$viewfullnames = has_capability('moodle/site:viewfullnames', $context);
		
		$enrolments = array();
		if($users = get_recent_enrolments($courseId, $timestart)){
			foreach($users as $user){
				$enrolment = new UedRecentEnrolment();
				$enrolment->courseId = $courseId;
				$enrolment->userId = $user->id;
				$enrolment->userFullName = fullname($user, $viewfullnames);
				$enrolment->date = new UedDate($user->time);
				$enrolment->userProfileLink = "{$CFG->wwwroot}/user/view.php?id={$user->id}&amp;course={$courseId}";
				
				$enrolments[] = $enrolment;
	        }
	    }
	    
	    return $enrolments;
    }
    
	/**
     * Return the recent modifications in a course
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @param int $courseId with the courseId to get recent activity from
     * @param UedDate $UedStartDate with the start date to get events from
     * @return UedRecentModification[] with all the recent modifications or throw a SoupFault on error
     */
    public static function getCourseRecentModifications($credentials, $courseId, $UedStartDate=NULL){
		global $CFG, $USER;
		
		$timestart = self::_getCourseRecent($credentials, $courseId, $UedStartDate);
		
		// load the course data
    	if (!$course = get_record('course', 'id', $courseId)) {
    		throw new SoapFault(self::$COURSENOTFOUND, get_string('uedws_coursenotfound', self::$LOCALENAME));
		}
		
		$modinfo =& get_fast_modinfo($course);
		$changelist = array();
		
		if($logs = get_records_select('log', "time > $timestart AND course = $course->id AND module = 'course' AND (action = 'add mod' OR action = 'update mod' OR action = 'delete mod')", "id ASC")){
			$actions  = array('add mod', 'update mod', 'delete mod');
			$newgones = array(); // added and later deleted items
			foreach($logs as $key=>$log){
				if(!in_array($log->action, $actions)){
					continue;
				}
				$info = split(' ', $log->info);
				
				if($info[0]=='label'){ // Labels are ignored in recent activity
					continue;
				}
				
				// Incorrect log entry info
				if(count($info)!=2){
					continue;
				}
				
				$modname=$info[0];
				$instanceid=$info[1];
				
				if($log->action=='delete mod'){
					// unfortunately we do not know if the mod was visible
					if(!array_key_exists($log->info, $newgones)){
						$recentModification = new UedRecentModification();
						$recentModification->operation = 'delete';
						$recentModification->text = get_string('deletedactivity', 'moodle', get_string('modulename', $modname));
						$recentModification->module = get_string('modulename', $modname);
						$recentModification->modulename = '';
						$recentModification->url = '';
						$recentModification->date = new UedDate($log->time);
						$changelist[] = $recentModification;
					}
				}else{
					if(!isset($modinfo->instances[$modname][$instanceid])){
						if($log->action=='add mod'){
							// do not display added and later deleted activities
							$newgones[$log->info] = true;
						}
						continue;
					}
					$cm=$modinfo->instances[$modname][$instanceid];
					if(!$cm->uservisible){
						continue;
					}
					if($log->action=='add mod'){
						$recentModification = new UedRecentModification();
						$recentModification->operation = 'add';
						$recentModification->text = get_string('added', 'moodle', get_string('modulename', $modname));
						$recentModification->module = get_string('modulename', $modname);
						$recentModification->modulename = format_string($cm->name, true);
						$recentModification->url = "{$CFG->wwwroot}/mod/{$cm->modname}/view.php?id={$cm->id}";
						$recentModification->date = new UedDate($log->time);
						$changelist[] = $recentModification;
						
					}elseif($log->action=='update mod' and empty($changelist[$log->info])){
						$recentModification = new UedRecentModification();
						$recentModification->operation = 'update';
						$recentModification->text = get_string('updated', 'moodle', get_string('modulename', $modname));
						$recentModification->module = get_string('modulename', $modname);
						$recentModification->modulename = format_string($cm->name, true);
						$recentModification->url = "{$CFG->wwwroot}/mod/{$cm->modname}/view.php?id={$cm->id}";
						$recentModification->date = new UedDate($log->time);
						$changelist[] = $recentModification;
						
					}
				}
			}
		}
	    return $changelist;
    }
    
	/*
     * Return the recent activity on the active modules within a course
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @param int $courseId with the courseId to get recent activity from
     * @param UedDate $UedStartDate with the start date to get events from
     * @return void[] with all the recent modifications or throw a SoupFault on error
     *//*// We'd like to enable this for all the module, but the content format is defined by each module without any rigid structure; for PHP this is OK, but for other plataforms (like .net and java) this can be a problem. Solution: use the case by case approach
    private static function getCourseRecentActivity($credentials, $courseId, $UedStartDate=NULL){
		global $CFG, $USER;
		
		$timestart = self::_getCourseRecent($credentials, $courseId, $UedStartDate);
		
		$context = get_context_instance(CONTEXT_COURSE, $courseId);
		$viewfullnames = has_capability('moodle/site:viewfullnames', $context);
		
		// load the course data
    	if (!$course = get_record('course', 'id', $courseId)) {
    		throw new SoapFault(self::$COURSENOTFOUND, get_string('uedws_coursenotfound', self::$LOCALENAME));
		}
		
		$modinfo =& get_fast_modinfo($course);
		
		$usedmodules = array();
		foreach($modinfo->cms as $cm){
			if(isset($usedmodules[$cm->modname])){
				continue;
			}
			if(!$cm->uservisible){
				continue;
			}
			$usedmodules[$cm->id] = $cm->modname;
		}
		
		
		$index=0;
		$activities = array();
		
		foreach($usedmodules as $cmid=>$modname){
			if(is_readable($CFG->dirroot.'/mod/'.$modname.'/lib.php')){
				include_once($CFG->dirroot.'/mod/'.$modname.'/lib.php');
				$libfunction = $modname."_get_recent_mod_activity";
				if(function_exists($libfunction)){
					$libfunction($activities, $index, $timestart, $courseId, $cmid);
				}
			}
		}
		
	    return $activities;
    }
    */
    
    /**
     * Return the recent forum activity within a course
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @param int $courseId with the courseId to get recent activity from
     * @param UedDate $UedStartDate with the start date to get events from
     * @return UedRecentForumActivity[] with all the recent modifications or throw a SoupFault on error
     */
    public static function getCourseRecentForumActivity($credentials, $courseId, $UedStartDate=NULL){
		global $CFG, $USER;
		
		$activities = self::_getCourseRecentActivity($credentials, $courseId, $UedStartDate, 'forum');
		$tmpUedRecentForumActivities = array();
		foreach($activities as $activity){
			if(!isset($tmpUedRecentForumActivities[$activity->cmid])){
				$uedRecentForumActivity = new UedRecentForumActivity();
				$uedRecentForumActivity->forumId = $activity->cmid;
				$uedRecentForumActivity->forumName = $activity->name;
				$uedRecentForumActivity->sectionNumber = $activity->sectionnum;
				$uedRecentForumActivity->activity = array();
				
				$tmpUedRecentForumActivities[$activity->cmid] = $uedRecentForumActivity;
			}
			// prepare the content
			$content = new UedForumActivity();
			$content->discussionType = (($activity->content->parent==0)?'discussion':'reply');
			$content->parentId = $activity->content->parent;
			$content->discussionId = $activity->content->discussion;
			$content->userId = $activity->user->id;
			$content->date = new UedDate($activity->timestamp);
			$content->postId = $activity->content->id;
			$content->postSubject = $activity->content->subject;
			$content->postUrl = "{$CFG->wwwroot}/mod/forum/discuss.php?d={$activity->content->discussion}";
			
			// add the content to the contents array
			$tmpUedRecentForumActivities[$uedRecentForumActivity->forumId]->activity[] = $content;
		}
		
		// Let's copy the activities to an unindexed array
    	$uedRecentForumActivities = array();
		foreach($tmpUedRecentForumActivities as $activity){
			$uedRecentForumActivities[] = $activity;
		}
			
	    return $uedRecentForumActivities;
    }
    
	/**
     * Get the public profile of a specific user
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @param int $userId with the user identifier
     * @param int $courseId with the course identifier of the common course were both users (author of the query and query destination are enroled); 1 is the default value for the site id
     * @return UedUser with the user profile or SoapFault
     */
    public static function getUserPublicProfile($credentials, $userId, $courseId=SITEID){
		global $CFG, $USER;
		if(self::_validate($credentials)){
			if(!$user=get_record("user", "id", $userId)){
				throw new SoapFault(self::$USERNOTFOUND, get_string('uedws_usernotfound', self::$LOCALENAME));
			}
			// get the user context
			$usercontext = get_context_instance(CONTEXT_USER, $user->id);
			if($courseId==SITEID){
				$coursecontext = get_context_instance(CONTEXT_SYSTEM);   // SYSTEM context
			}else{
				$coursecontext = get_context_instance(CONTEXT_COURSE, $course->id);   // Course context
			}
			
			// Check for permissions to see this
			if(!has_capability('moodle/user:viewdetails', $coursecontext) && !has_capability('moodle/user:viewdetails', $usercontext)){
				throw new SoapFault(self::$INSUFFICIENTPERMISSIONS, get_string('uedws_insufficientpermissionstoseetheuserprofile', self::$LOCALENAME));
			}
			
			if(has_capability('moodle/user:viewhiddendetails', $coursecontext)){
				$hiddenfields=array();
			}else{
				$hiddenfields = array_flip(explode(',', $CFG->hiddenuserfields));
			}
			
			require_once($CFG->libdir.'/filelib.php');
			
			$userObject = new UedUser();
			$userObject->id = $user->id;
			$userObject->fullName = fullname($user, has_capability('moodle/site:viewfullnames', $coursecontext));
			$userObject->imageUrl = (($user->picture!=0)?get_file_url($user->id.'/f1.jpg', null, 'user'):"{$CFG->pixpath}/u/f1.png");
			$userObject->description = (($user->description && !isset($hiddenfields['description']))?$user->description:'');
			$userObject->country = (($user->country && !isset($hiddenfields['country']))?$user->country:'');
			$userObject->city = (($user->city && !isset($hiddenfields['city']))?$user->city:'');
			$userObject->webPageUrl = (($user->url && !isset($hiddenfields['webpage']))?$user->url:'');
			$userObject->profileLink = "{$CFG->wwwroot}/user/view.php?id={$user->id}";
			
			return $userObject;
        }
        
        throw new SoapFault(self::$AUTHENTICATIONERROR, get_string('uedws_authenticationerror', self::$LOCALENAME));
    }
    
	/**
     * Get the user profile
     *
     * @param UedCredentials $credentials with the username and autorization key
     * @return UedUser with the user profile or SoapFault
     */
    public static function getMyUserPublicProfile($credentials){
		global $CFG, $USER;
		
		if(self::_validate($credentials)):
			require_once($CFG->libdir.'/filelib.php');
			
			$userObject = new UedUser();
			$userObject->id = $USER->id;
			$userObject->fullName = "{$USER->firstname} {$USER->lastname}";
			$userObject->imageUrl = (($USER->picture!=0)?get_file_url($USER->id.'/f1.jpg', null, 'user'):"{$CFG->pixpath}/u/f1.png");
			$userObject->description = (($USER->description)?$USER->description:'');
			$userObject->country = (($USER->country)?$USER->country:'');
			$userObject->city = (($USER->city)?$USER->city:'');
			$userObject->webPageUrl = (($USER->url)?$USER->url:'');
			$userObject->profileLink = "{$CFG->wwwroot}/user/view.php?id={$USER->id}";
			
			return $userObject;
		endif;
		throw new SoapFault(self::$AUTHENTICATIONERROR, get_string('uedws_authenticationerror', self::$LOCALENAME));
    }
    
	/**
     * Get the webservice plugin version
     *
	 * @return string with the plugin version
     */
    public static function getVersion(){
    	if(!is_enabled_auth('uedws')){
    		throw new SoapFault(self::$UEDWSAUTHENTICATIONPLUGINDISABLED, get_string('uedws_theuedwsauthenticationpluginisdisabled', self::$LOCALENAME));
    	}
    	return get_config(NULL, self::$LOCALENAME.'_version');
    }
}
    
