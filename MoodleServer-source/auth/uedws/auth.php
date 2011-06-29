<?php

/**
 * Upgrade file to handle the module database upgrade
 * 
 * @package		UEDWS
 * @subpackage	auth_plugin_uedws
 * @version		2010.0
 * @uses 		auth_plugin_base
 * @copyright 	Learning Distance Unit {@link http://ued.ipleiria.pt}, Polytechnic Institute of Leiria
 * @author		Cláudio Esperança <cesperanc@gmail.com>
 * @license 	GNU GPL v3 or later {@link http://www.gnu.org/copyleft/gpl.html}
 */

//  It must be included from a Moodle page
if (!defined('MOODLE_INTERNAL')) {
    die('');
}

require_once($CFG->libdir.'/authlib.php');

define('AUTH_PLUGIN_NAME', 'auth/uedws');
define('AUTH_PLUGIN_LANG', 'auth_uedws');

/**
 * Main authentication class
 * 
 * @author Cláudio Esperança
 */
class auth_plugin_uedws extends auth_plugin_base {
	/**
     * Constructor.
     */
    function auth_plugin_uedws() {
        global $USER;
        $this->authtype = 'uedws';
    }
	
	/**
     * Returns true if the username and authentication key match, false otherwise
     *
     * @param string $username with the username
     * @param string $authenticationKey with the token
     * @return bool true on success, false otherwise
     */
    function user_login($username, $authenticationKey) {
    	if(!empty($username) && !empty($authenticationKey)){
    		if($records = get_records('uedws_authentication', 'autorizationkey', $authenticationKey)){
	    		foreach($records as $record){
	    			if($record->username == $username && get_record('user', 'username', $username)){
	    				return true;
	    			}
	    		}
    		}
    	}
    	
        return false;
    }
	
	/**
     * Returns true if this authentication plugin is 'internal'.
     *
     * @return bool
     */
    function is_internal() {
        return true;
    }

    /**
     * Returns true if this authentication plugin can change the user's
     * password.
     *
     * @return bool
     */
    function can_change_password() {
        return false;
    }

    /**
     * Returns the URL for changing the user's pw, or false if the default can
     * be used.
     *
     * @return bool
     */
    function change_password_url() {
        return false;
    }
	
	/**
     * Hook for overriding behavior of login page.
     * 
     * An URL in the format {moodlehost}/login/index.php?ueduser={username}&uedtoken={authenticationKey}&goto={destinationURL} can authenticate the user
     * @example http://localhost/login/index.php?ueduser=cesperanc&uedtoken=9aaf3d8d11aed1cc5729d2518faf57719036e3b573eda68c5eed428a9e83d041&goto=admin/
     */
    function loginpage_hook() {
        global $CFG, $frm, $user;
        
        $authenticationKey = optional_param('uedtoken', null);
        $username = optional_param('ueduser', null);
        $destinationUrl = optional_param('goto', null);
        if(!empty($username) && !empty($authenticationKey) && !empty($destinationUrl))
        if($user = self::authenticateUedUser($username, $authenticationKey)){
        	add_to_log(SITEID, 'user', 'login', "view.php?id=$user->id&course=".SITEID, get_string('theauthenticationfortheuserwascompletedwithsuccess', AUTH_PLUGIN_LANG, $user->username),0,$user->id);
	                
            if(!empty($destinationUrl)){
                $urltogo = "$CFG->wwwroot/$destinationUrl";
            }else if (isset($SESSION->wantsurl) and (strpos($SESSION->wantsurl, $CFG->wwwroot) === 0)) {
                $urltogo = $SESSION->wantsurl;
            }else{
            	$urltogo = $CFG->wwwroot.'/';
            }
            unset($SESSION->wantsurl);
            
            redirect($urltogo, get_string('hellouser', AUTH_PLUGIN_LANG, $user->username),0);
            exit();
        }else{
        	error_log(get_string('theuseraccountdoesnotexistsinmoodlelog', AUTH_PLUGIN_LANG, $username), E_USER_ERROR);
			redirect($CFG->wwwroot.'/',get_string('theuseraccountdoesnotexistsinmoodle', AUTH_PLUGIN_LANG, $username),5);
			exit();
        }
        
        return false;
    }
    
	/**
     * Validates and authenticates the user
     * 
     * An URL in the format {moodlehost}/login/index.php?ueduser={username}&uedtoken={authenticationKey}&goto={destinationURL} can authenticate the user
     * @example http://localhost/login/index.php?ueduser=cesperanc&uedtoken=9aaf3d8d11aed1cc5729d2518faf57719036e3b573eda68c5eed428a9e83d041&goto=admin/
     */
    function authenticateUedUser($username=null, $authenticationKey=null) {
        global $CFG, $USER, $user;
        
        if(!empty($username) && self::user_login($username, $authenticationKey)){
        	if($user = get_complete_user_data('username', $username)){
            	$USER = $user = complete_user_login($user);
            	
            	return $user;
            }
        }
        return false;
    }
}